import 'dart:convert';

import 'package:shared_preferences/shared_preferences.dart';

const _tokenKey = 'aqualog_token';

class DecodedToken {
  final String? username;
  final String? role;

  DecodedToken({this.username, this.role});
}

class TokenStore {
  Future<String?> read() async {
    final prefs = await SharedPreferences.getInstance();
    return prefs.getString(_tokenKey);
  }

  Future<void> write(String token) async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.setString(_tokenKey, token);
  }

  Future<void> clear() async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.remove(_tokenKey);
  }
}

/// The role claim reflects the user's real Identity role (server-side fix
/// landed 2026-09-05) but is still only meaningful for display here — the
/// server enforces its own [Authorize(Roles = ...)] checks.
DecodedToken decodeToken(String token) {
  try {
    final parts = token.split('.');
    var payload = parts[1].replaceAll('-', '+').replaceAll('_', '/');
    payload = payload.padRight((payload.length + 3) ~/ 4 * 4, '=');
    final json = jsonDecode(utf8.decode(base64.decode(payload))) as Map<String, dynamic>;

    return DecodedToken(
      username: json['unique_name'] as String? ?? json['name'] as String?,
      role: json['role'] as String?,
    );
  } catch (_) {
    return DecodedToken();
  }
}
