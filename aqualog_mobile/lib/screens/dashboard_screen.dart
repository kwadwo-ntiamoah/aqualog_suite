import 'package:flutter/material.dart';

import '../api/api_client.dart';
import '../theme.dart';

class Me {
  final String userId;
  final String username;
  final String fullname;
  final String? shopId;
  final String? shopName;
  final String role;

  Me({
    required this.userId,
    required this.username,
    required this.fullname,
    required this.shopId,
    required this.shopName,
    required this.role,
  });

  factory Me.fromJson(Map<String, dynamic> json) => Me(
        userId: json['userId'] as String,
        username: json['username'] as String,
        fullname: json['fullname'] as String,
        shopId: json['shopId'] as String?,
        shopName: json['shopName'] as String?,
        role: json['role'] as String,
      );
}

class BalanceInfo {
  final bool hasRecord;
  final num balance;
  final DateTime? dateRecorded;
  final String? recordedByName;

  BalanceInfo({required this.hasRecord, required this.balance, required this.dateRecorded, required this.recordedByName});

  factory BalanceInfo.fromJson(Map<String, dynamic> json) => BalanceInfo(
        hasRecord: json['hasRecord'] as bool,
        balance: json['balance'] as num,
        dateRecorded: json['dateRecorded'] != null ? DateTime.parse(json['dateRecorded'] as String) : null,
        recordedByName: json['recordedByName'] as String?,
      );
}

class ShopSummary {
  final int salesToday;
  final num collectedToday;

  ShopSummary({required this.salesToday, required this.collectedToday});

  factory ShopSummary.fromJson(Map<String, dynamic> json) =>
      ShopSummary(salesToday: json['salesToday'] as int, collectedToday: json['collectedToday'] as num);
}

bool _isToday(DateTime? date) {
  if (date == null) return false;
  final local = date.toLocal();
  final now = DateTime.now();
  return local.year == now.year && local.month == now.month && local.day == now.day;
}

String _greeting() {
  final hour = DateTime.now().hour;
  if (hour < 12) return 'Good morning';
  if (hour < 18) return 'Good afternoon';
  return 'Good evening';
}

class DashboardScreen extends StatefulWidget {
  const DashboardScreen({super.key});

  @override
  State<DashboardScreen> createState() => _DashboardScreenState();
}

class _DashboardScreenState extends State<DashboardScreen> {
  Me? _me;
  BalanceInfo? _balance;
  ShopSummary? _summary;
  String? _loadError;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() => _loadError = null);
    try {
      final meJson = await apiGet('/auth/me') as Map<String, dynamic>;
      final me = Me.fromJson(meJson);

      BalanceInfo? balance;
      ShopSummary? summary;
      if (me.shopId != null) {
        final results = await Future.wait([
          apiGet('/shop/${me.shopId}/balance'),
          apiGet('/shop/${me.shopId}/summary'),
        ]);
        balance = BalanceInfo.fromJson(results[0] as Map<String, dynamic>);
        summary = ShopSummary.fromJson(results[1] as Map<String, dynamic>);
      }

      if (!mounted) return;
      setState(() {
        _me = me;
        _balance = balance;
        _summary = summary;
      });
    } on ApiException catch (e) {
      if (mounted) setState(() => _loadError = e.message);
    } catch (_) {
      if (mounted) setState(() => _loadError = "Couldn't load your dashboard.");
    }
  }

  @override
  Widget build(BuildContext context) {
    if (_loadError != null) {
      return _ErrorBanner(message: _loadError!);
    }

    final me = _me;
    if (me == null) {
      return const Center(child: CircularProgressIndicator(color: AppColors.primary));
    }

    if (me.shopId == null) {
      return const Center(
        child: Padding(
          padding: EdgeInsets.all(24),
          child: Card(
            child: Padding(
              padding: EdgeInsets.all(20),
              child: Column(
                mainAxisSize: MainAxisSize.min,
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text('No shop assigned', style: TextStyle(fontWeight: FontWeight.bold, fontSize: 16)),
                  SizedBox(height: 8),
                  Text(
                    "Your account isn't assigned to a shop yet. Contact your admin.",
                    style: TextStyle(color: AppColors.mutedForeground),
                  ),
                ],
              ),
            ),
          ),
        ),
      );
    }

    final balanceSetToday = _balance != null && _balance!.hasRecord && _isToday(_balance!.dateRecorded);

    if (!balanceSetToday) {
      return Center(
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(24),
          child: Card(
            child: Padding(
              padding: const EdgeInsets.all(20),
              child: Column(
                mainAxisSize: MainAxisSize.min,
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const Text("Set today's electricity balance", style: TextStyle(fontWeight: FontWeight.bold, fontSize: 16)),
                  const SizedBox(height: 4),
                  const Text(
                    'This is required once per day before you can make sales.',
                    style: TextStyle(color: AppColors.mutedForeground),
                  ),
                  const SizedBox(height: 16),
                  _BalanceForm(shopId: me.shopId!, onSaved: _load),
                ],
              ),
            ),
          ),
        ),
      );
    }

    return RefreshIndicator(
      onRefresh: _load,
      child: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          Container(
            padding: const EdgeInsets.all(16),
            decoration: BoxDecoration(color: AppColors.primary, borderRadius: BorderRadius.circular(16)),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  '${_greeting()}, ${me.fullname}',
                  style: const TextStyle(color: Colors.white, fontWeight: FontWeight.bold, fontSize: 17),
                ),
                const SizedBox(height: 2),
                Text(
                  _formatFullDate(DateTime.now()),
                  style: const TextStyle(color: Colors.white70, fontSize: 13),
                ),
              ],
            ),
          ),
          const SizedBox(height: 12),
          Row(
            children: [
              Expanded(child: _StatCard(value: '${_summary?.salesToday ?? 0}', label: 'Sales today')),
              const SizedBox(width: 12),
              Expanded(
                child: _StatCard(
                  value: 'GHS ${(_summary?.collectedToday ?? 0).toStringAsFixed(2)}',
                  label: 'Collected today',
                ),
              ),
            ],
          ),
          const SizedBox(height: 12),
          Card(
            child: Padding(
              padding: const EdgeInsets.all(16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      const Text(
                        'ELECTRICITY BALANCE',
                        style: TextStyle(fontSize: 11, fontWeight: FontWeight.w700, color: AppColors.mutedForeground, letterSpacing: 0.5),
                      ),
                      Text(
                        'GHS ${(_balance?.balance ?? 0).toStringAsFixed(2)}',
                        style: const TextStyle(fontSize: 17, fontWeight: FontWeight.bold, color: AppColors.warning),
                      ),
                    ],
                  ),
                  const SizedBox(height: 12),
                  _BalanceForm(shopId: me.shopId!, onSaved: _load, compact: true),
                  const SizedBox(height: 8),
                  const Text(
                    'No admin approval needed — update as often as needed.',
                    style: TextStyle(fontSize: 11, fontStyle: FontStyle.italic, color: AppColors.mutedForeground),
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

String _formatFullDate(DateTime date) {
  const weekdays = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday'];
  const months = [
    'January', 'February', 'March', 'April', 'May', 'June',
    'July', 'August', 'September', 'October', 'November', 'December',
  ];
  return '${weekdays[date.weekday - 1]}, ${months[date.month - 1]} ${date.day}, ${date.year}';
}

class _StatCard extends StatelessWidget {
  final String value;
  final String label;

  const _StatCard({required this.value, required this.label});

  @override
  Widget build(BuildContext context) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(value, style: const TextStyle(fontSize: 20, fontWeight: FontWeight.bold)),
            const SizedBox(height: 2),
            Text(
              label.toUpperCase(),
              style: const TextStyle(fontSize: 10, fontWeight: FontWeight.w700, color: AppColors.mutedForeground, letterSpacing: 0.5),
            ),
          ],
        ),
      ),
    );
  }
}

class _BalanceForm extends StatefulWidget {
  final String shopId;
  final VoidCallback onSaved;
  final bool compact;

  const _BalanceForm({required this.shopId, required this.onSaved, this.compact = false});

  @override
  State<_BalanceForm> createState() => _BalanceFormState();
}

class _BalanceFormState extends State<_BalanceForm> {
  final _controller = TextEditingController();
  bool _submitting = false;
  String? _error;

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    setState(() => _error = null);

    final value = int.tryParse(_controller.text.trim());
    if (value == null || value < 0) {
      setState(() => _error = 'Enter a valid balance');
      return;
    }

    setState(() => _submitting = true);
    try {
      await apiPost('/shop/${widget.shopId}/balance', body: {'balance': value});
      _controller.clear();
      widget.onSaved();
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
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        if (!widget.compact) ...[
          const Text('CURRENT BALANCE', style: TextStyle(fontSize: 11, fontWeight: FontWeight.w700, color: AppColors.mutedForeground)),
          const SizedBox(height: 6),
        ],
        TextField(
          controller: _controller,
          keyboardType: TextInputType.number,
          decoration: const InputDecoration(hintText: 'Enter new balance'),
        ),
        if (_error != null) ...[
          const SizedBox(height: 6),
          Text(_error!, style: const TextStyle(color: AppColors.destructive, fontSize: 12)),
        ],
        const SizedBox(height: 10),
        ElevatedButton(
          onPressed: _submitting ? null : _submit,
          child: Text(_submitting ? 'Saving...' : 'Update'),
        ),
      ],
    );
  }
}

class _ErrorBanner extends StatelessWidget {
  final String message;

  const _ErrorBanner({required this.message});

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(24),
        child: Container(
          padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
          decoration: BoxDecoration(
            color: AppColors.destructive.withValues(alpha: 0.1),
            border: Border.all(color: AppColors.destructive.withValues(alpha: 0.2)),
            borderRadius: BorderRadius.circular(8),
          ),
          child: Text(message, style: const TextStyle(color: AppColors.destructive)),
        ),
      ),
    );
  }
}
