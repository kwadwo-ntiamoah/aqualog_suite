import 'package:flutter/material.dart';

import 'api/api_client.dart';
import 'screens/sign_in_screen.dart';
import 'theme.dart';
import 'widgets/app_shell.dart';

void main() {
  runApp(const AquaLogSalesApp());
}

class AquaLogSalesApp extends StatelessWidget {
  const AquaLogSalesApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'AquaLog Sales',
      debugShowCheckedModeBanner: false,
      theme: buildAppTheme(),
      home: const AuthGate(),
    );
  }
}

/// Checks for a stored session on launch and routes to sign-in or the
/// authenticated shell — the mobile equivalent of aqualog_web's per-route
/// auth guard, just centralized here since Flutter has no URL-based routes.
class AuthGate extends StatefulWidget {
  const AuthGate({super.key});

  @override
  State<AuthGate> createState() => _AuthGateState();
}

class _AuthGateState extends State<AuthGate> {
  bool _loading = true;
  bool _signedIn = false;

  @override
  void initState() {
    super.initState();
    _checkSession();
  }

  Future<void> _checkSession() async {
    final token = await getStoredToken();
    setState(() {
      _signedIn = token != null;
      _loading = false;
    });
  }

  @override
  Widget build(BuildContext context) {
    if (_loading) {
      return const Scaffold(body: Center(child: CircularProgressIndicator(color: AppColors.primary)));
    }

    if (!_signedIn) {
      return SignInScreen(onSignedIn: () => setState(() => _signedIn = true));
    }

    return AppShell(onSignedOut: () => setState(() => _signedIn = false));
  }
}
