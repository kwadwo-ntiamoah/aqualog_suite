import 'package:flutter/material.dart';

import '../api/api_client.dart';
import '../theme.dart';

class RequestVehicleScreen extends StatefulWidget {
  const RequestVehicleScreen({super.key});

  @override
  State<RequestVehicleScreen> createState() => _RequestVehicleScreenState();
}

class _RequestVehicleScreenState extends State<RequestVehicleScreen> {
  final _nameController = TextEditingController();
  final _regNoController = TextEditingController();
  final _contactController = TextEditingController();
  final _tanksController = TextEditingController();

  bool _submitting = false;
  String? _error;
  String? _success;

  @override
  void dispose() {
    _nameController.dispose();
    _regNoController.dispose();
    _contactController.dispose();
    _tanksController.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    setState(() {
      _error = null;
      _success = null;
    });

    final name = _nameController.text.trim();
    final regNo = _regNoController.text.trim();
    final contact = _contactController.text.trim();
    final tanksInTruck = int.tryParse(_tanksController.text.trim());

    if (name.isEmpty) {
      setState(() => _error = 'Enter a driver name');
      return;
    }
    if (regNo.isEmpty) {
      setState(() => _error = 'Enter a vehicle number');
      return;
    }
    if (contact.isEmpty) {
      setState(() => _error = 'Enter a contact number');
      return;
    }
    if (tanksInTruck == null || tanksInTruck < 1) {
      setState(() => _error = 'Enter the tank capacity (must be at least 1)');
      return;
    }

    setState(() => _submitting = true);
    try {
      await apiPost(
        '/driver/request',
        body: {
          'name': name,
          'regNo': regNo,
          'contact': contact,
          'tanksInTruck': tanksInTruck,
        },
      );

      setState(() {
        _success = '"$name" ($regNo) was submitted for admin approval.';
      });
      _nameController.clear();
      _regNoController.clear();
      _contactController.clear();
      _tanksController.clear();
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
                    'REQUEST NEW VEHICLE',
                    style: TextStyle(
                      fontSize: 11,
                      fontWeight: FontWeight.w700,
                      color: AppColors.mutedForeground,
                    ),
                  ),
                  const SizedBox(height: 4),
                  const Text(
                    "This gets sent to your admin for approval — it won't be usable for sales until then.",
                    style: TextStyle(fontSize: 13, color: AppColors.mutedForeground),
                  ),
                  const SizedBox(height: 16),
                  const Text(
                    'DRIVER NAME',
                    style: TextStyle(
                      fontSize: 11,
                      fontWeight: FontWeight.w700,
                      color: AppColors.mutedForeground,
                    ),
                  ),
                  const SizedBox(height: 6),
                  TextField(controller: _nameController),
                  const SizedBox(height: 14),
                  const Text(
                    'VEHICLE NUMBER',
                    style: TextStyle(
                      fontSize: 11,
                      fontWeight: FontWeight.w700,
                      color: AppColors.mutedForeground,
                    ),
                  ),
                  const SizedBox(height: 6),
                  TextField(controller: _regNoController),
                  const SizedBox(height: 14),
                  const Text(
                    'CONTACT NUMBER',
                    style: TextStyle(
                      fontSize: 11,
                      fontWeight: FontWeight.w700,
                      color: AppColors.mutedForeground,
                    ),
                  ),
                  const SizedBox(height: 6),
                  TextField(controller: _contactController, keyboardType: TextInputType.phone),
                  const SizedBox(height: 14),
                  const Text(
                    'TANKS IN TRUCK (CAPACITY)',
                    style: TextStyle(
                      fontSize: 11,
                      fontWeight: FontWeight.w700,
                      color: AppColors.mutedForeground,
                    ),
                  ),
                  const SizedBox(height: 6),
                  TextField(controller: _tanksController, keyboardType: TextInputType.number),
                  if (_error != null) ...[
                    const SizedBox(height: 12),
                    _ErrorText(_error!),
                  ],
                  if (_success != null) ...[
                    const SizedBox(height: 12),
                    Container(
                      width: double.infinity,
                      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
                      decoration: BoxDecoration(
                        color: AppColors.primary.withValues(alpha: 0.1),
                        borderRadius: BorderRadius.circular(8),
                      ),
                      child: Text(_success!, style: const TextStyle(color: AppColors.primary, fontSize: 13)),
                    ),
                  ],
                  const SizedBox(height: 16),
                  ElevatedButton(
                    onPressed: _submitting ? null : _submit,
                    child: Text(_submitting ? 'Submitting...' : 'Submit for approval'),
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

class _ErrorText extends StatelessWidget {
  final String message;

  const _ErrorText(this.message);

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
      decoration: BoxDecoration(
        color: AppColors.destructive.withValues(alpha: 0.1),
        border: Border.all(color: AppColors.destructive.withValues(alpha: 0.2)),
        borderRadius: BorderRadius.circular(8),
      ),
      child: Text(message, style: const TextStyle(color: AppColors.destructive)),
    );
  }
}
