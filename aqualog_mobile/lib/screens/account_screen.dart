import 'package:flutter/material.dart';

import '../api/api_client.dart';
import '../api/token_store.dart';
import '../theme.dart';

class AccountScreen extends StatefulWidget {
  const AccountScreen({super.key});

  @override
  State<AccountScreen> createState() => _AccountScreenState();
}

class _AccountScreenState extends State<AccountScreen> {
  final _usernameController = TextEditingController();
  final _oldPasswordController = TextEditingController();
  final _newPasswordController = TextEditingController();

  bool _submitting = false;
  String? _error;
  bool _success = false;

  @override
  void initState() {
    super.initState();
    _loadUsername();
  }

  Future<void> _loadUsername() async {
    final token = await getStoredToken();
    if (token == null) return;
    final decoded = decodeToken(token);
    if (mounted) setState(() => _usernameController.text = decoded.username ?? '');
  }

  @override
  void dispose() {
    _usernameController.dispose();
    _oldPasswordController.dispose();
    _newPasswordController.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    setState(() {
      _error = null;
      _success = false;
    });

    final oldPassword = _oldPasswordController.text;
    final newPassword = _newPasswordController.text;

    if (oldPassword.isEmpty) {
      setState(() => _error = 'Enter your current password');
      return;
    }
    if (newPassword.length < 8) {
      setState(() => _error = 'New password must be at least 8 characters');
      return;
    }

    setState(() => _submitting = true);
    try {
      await apiPost(
        '/auth/password/change',
        body: {'oldPassword': oldPassword, 'newPassword': newPassword},
      );

      _oldPasswordController.clear();
      _newPasswordController.clear();
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
    return SingleChildScrollView(
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Card(
            child: Padding(
              padding: const EdgeInsets.all(16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  const Text(
                    'SIGNED IN AS',
                    style: TextStyle(
                      fontSize: 11,
                      fontWeight: FontWeight.w700,
                      color: AppColors.mutedForeground,
                    ),
                  ),
                  const SizedBox(height: 8),
                  const Text(
                    'USER ID',
                    style: TextStyle(fontSize: 11, fontWeight: FontWeight.w700, color: AppColors.mutedForeground),
                  ),
                  const SizedBox(height: 6),
                  TextField(
                    enabled: false,
                    controller: _usernameController,
                  ),
                ],
              ),
            ),
          ),
          const SizedBox(height: 12),
          Card(
            child: Padding(
              padding: const EdgeInsets.all(16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  const Text(
                    'CHANGE PASSWORD',
                    style: TextStyle(
                      fontSize: 11,
                      fontWeight: FontWeight.w700,
                      color: AppColors.mutedForeground,
                    ),
                  ),
                  const SizedBox(height: 12),
                  const Text(
                    'CURRENT PASSWORD',
                    style: TextStyle(fontSize: 11, fontWeight: FontWeight.w700, color: AppColors.mutedForeground),
                  ),
                  const SizedBox(height: 6),
                  TextField(controller: _oldPasswordController, obscureText: true),
                  const SizedBox(height: 14),
                  const Text(
                    'NEW PASSWORD',
                    style: TextStyle(fontSize: 11, fontWeight: FontWeight.w700, color: AppColors.mutedForeground),
                  ),
                  const SizedBox(height: 6),
                  TextField(controller: _newPasswordController, obscureText: true),
                  if (_error != null) ...[
                    const SizedBox(height: 12),
                    Container(
                      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
                      decoration: BoxDecoration(
                        color: AppColors.destructive.withValues(alpha: 0.1),
                        border: Border.all(color: AppColors.destructive.withValues(alpha: 0.2)),
                        borderRadius: BorderRadius.circular(8),
                      ),
                      child: Text(_error!, style: const TextStyle(color: AppColors.destructive)),
                    ),
                  ],
                  if (_success) ...[
                    const SizedBox(height: 12),
                    Container(
                      width: double.infinity,
                      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
                      decoration: BoxDecoration(
                        color: AppColors.primary.withValues(alpha: 0.1),
                        borderRadius: BorderRadius.circular(8),
                      ),
                      child: const Text('Password updated.', style: TextStyle(color: AppColors.primary, fontSize: 13)),
                    ),
                  ],
                  const SizedBox(height: 16),
                  ElevatedButton(
                    onPressed: _submitting ? null : _submit,
                    child: Text(_submitting ? 'Updating...' : 'Update password'),
                  ),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }
}
