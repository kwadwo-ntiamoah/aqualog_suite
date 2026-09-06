import 'dart:convert';

import 'package:http/http.dart' as http;

import 'token_store.dart';

/// http://localhost works from the iOS Simulator (shares the host Mac's
/// loopback). A physical device or Android emulator needs a real host
/// address instead (Android emulator: 10.0.2.2).
const String apiBaseUrl = 'http://localhost:5260/api';

final TokenStore _tokenStore = TokenStore();
final http.Client _httpClient = http.Client();

class ApiException implements Exception {
  final int status;
  final String message;

  ApiException(this.status, this.message);

  @override
  String toString() => message;
}

Future<String?> getStoredToken() => _tokenStore.read();

Future<void> setStoredToken(String token) => _tokenStore.write(token);

Future<void> clearStoredToken() => _tokenStore.clear();

Future<dynamic> apiGet(String path, {Map<String, String>? query}) {
  return _send('GET', path, query: query);
}

Future<dynamic> apiPost(String path, {Object? body}) {
  return _send('POST', path, body: body);
}

/// Downloads a raw file (CSV/Excel export) rather than parsing it as JSON.
Future<http.Response> apiGetRaw(String path, {Map<String, String>? query}) async {
  final uri = _buildUri(path, query);
  final headers = await _authHeaders();

  final response = await _httpClient.get(uri, headers: headers);

  if (response.statusCode == 401) await _tokenStore.clear();
  if (response.statusCode < 200 || response.statusCode >= 300) {
    throw ApiException(response.statusCode, _extractErrorMessage(response));
  }

  return response;
}

Uri _buildUri(String path, Map<String, String>? query) {
  final uri = Uri.parse('$apiBaseUrl$path');
  if (query == null || query.isEmpty) return uri;
  return uri.replace(queryParameters: {...uri.queryParameters, ...query});
}

Future<Map<String, String>> _authHeaders() async {
  final token = await _tokenStore.read();
  return {if (token != null) 'Authorization': 'Bearer $token'};
}

Future<dynamic> _send(String method, String path, {Map<String, String>? query, Object? body}) async {
  final uri = _buildUri(path, query);
  final headers = await _authHeaders();
  if (body != null) headers['Content-Type'] = 'application/json';

  final request = http.Request(method, uri)..headers.addAll(headers);
  if (body != null) request.body = jsonEncode(body);

  final streamed = await _httpClient.send(request);
  final response = await http.Response.fromStream(streamed);

  if (response.statusCode == 401) {
    await _tokenStore.clear();
  }

  if (response.statusCode < 200 || response.statusCode >= 300) {
    throw ApiException(response.statusCode, _extractErrorMessage(response));
  }

  if (response.statusCode == 204 || response.body.isEmpty) return null;
  return jsonDecode(response.body);
}

// A field-validation response (ASP.NET's ValidationProblem) always sets
// `title` to a generic "One or more validation errors occurred." and buries
// the actual message inside `errors` — check that first whenever it's
// present, since it's strictly more specific than `title`. (Same fix as
// aqualog_web's lib/api-client.ts — this API quirk applies to every client.)
String _extractErrorMessage(http.Response response) {
  try {
    final data = jsonDecode(response.body);

    if (data is Map && data['errors'] is Map) {
      final errors = data['errors'] as Map;
      if (errors.isNotEmpty) {
        final firstError = errors.values.first;
        if (firstError is List && firstError.isNotEmpty) return firstError.first.toString();
      }
    }

    if (data is Map && data['title'] is String) return data['title'] as String;
    if (data is String) return data;
  } catch (_) {
    // Response body wasn't JSON — fall through to the status line.
  }

  return response.reasonPhrase ?? 'Something went wrong';
}
