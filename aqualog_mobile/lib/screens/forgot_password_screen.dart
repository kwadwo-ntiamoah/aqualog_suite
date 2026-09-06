import 'package:flutter/material.dart';

import '../api/api_client.dart';
import '../theme.dart';

class ForgotPasswordScreen extends StatefulWidget {
  const ForgotPasswordScreen({super.key});

  @override
  State<ForgotPasswordScreen> createState() => _ForgotPasswordScreenState();
}

class _ForgotPasswordScreenState extends State<ForgotPasswordScreen> {
  final _usernameController = TextEditingController();
  bool _submitting = false;
  String? _error;
  bool _success = false;

  @override
  void dispose() {
    _usernameController.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    setState(() => _error = null);

    final username = _usernameController.text.trim();
    if (username.isEmpty) {
      setState(() => _error = 'Enter your User ID');
      return;
    }

    setState(() => _submitting = true);
    try {
      await apiPost('/auth/password/reset-request', body: {'username': username});
      setState(() => _success = true);
    } on ApiException catch (e) {
      setState(() => _error = e.message);
    } catch (_) {
      setState(() => _error = 'Something went wrong. Try again.');
    } finally {
      if (mounted) setState(() => _submitting = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Forgot password')),
      body: SafeArea(
        child: Center(
          child: SingleChildScrollView(
            padding: const EdgeInsets.all(24),
            child: Card(
              child: Padding(
                padding: const EdgeInsets.all(24),
                child: Column(
                  mainAxisSize: MainAxisSize.min,
                  crossAxisAlignment: CrossAxisAlignment.center,
                  children: [
                    Container(
                      width: 40,
                      height: 40,
                      decoration: const BoxDecoration(color: AppColors.primary, shape: BoxShape.circle),
                    ),
                    const SizedBox(height: 10),
                    const Text('Reset your password', style: TextStyle(fontWeight: FontWeight.bold, fontSize: 16)),
                    const SizedBox(height: 4),
                    const Text(
                      'Enter your User ID and your admin will be notified.',
                      textAlign: TextAlign.center,
                      style: TextStyle(fontSize: 13, color: AppColors.mutedForeground),
                    ),
                    const SizedBox(height: 20),
                    if (_success)
                      Container(
                        width: double.infinity,
                        padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
                        decoration: BoxDecoration(
                          color: AppColors.primary.withValues(alpha: 0.1),
                          borderRadius: BorderRadius.circular(8),
                        ),
                        child: const Text(
                          'Your admin has been notified.',
                          style: TextStyle(color: AppColors.primary, fontSize: 13),
                        ),
                      )
                    else
                      Column(
                        crossAxisAlignment: CrossAxisAlignment.stretch,
                        children: [
                          const Text(
                            'USER ID',
                            style: TextStyle(fontSize: 11, fontWeight: FontWeight.w700, color: AppColors.mutedForeground),
                          ),
                          const SizedBox(height: 6),
                          TextField(controller: _usernameController, onSubmitted: (_) => _submit()),
                          if (_error != null) ...[
                            const SizedBox(height: 8),
                            Container(
                              width: double.infinity,
                              padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
                              decoration: BoxDecoration(
                                color: AppColors.destructive.withValues(alpha: 0.1),
                                border: Border.all(color: AppColors.destructive.withValues(alpha: 0.2)),
                                borderRadius: BorderRadius.circular(8),
                              ),
                              child: Text(_error!, style: const TextStyle(color: AppColors.destructive, fontSize: 13)),
                            ),
                          ],
                          const SizedBox(height: 16),
                          ElevatedButton(
                            onPressed: _submitting ? null : _submit,
                            child: Text(_submitting ? 'Sending...' : 'Notify admin'),
                          ),
                        ],
                      ),
                  ],
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }
}
