import 'package:flutter/material.dart';

import '../api/api_client.dart';
import '../theme.dart';

class ContainerPrice {
  final String type;
  final num unitPrice;

  ContainerPrice({required this.type, required this.unitPrice});

  factory ContainerPrice.fromJson(Map<String, dynamic> json) => ContainerPrice(
    type: json['type'] as String,
    unitPrice: json['unitPrice'] as num,
  );
}

class Driver {
  final String name;
  final String contact;
  final int tanksInTruck;
  final String regNo;

  Driver({
    required this.name,
    required this.contact,
    required this.tanksInTruck,
    required this.regNo,
  });

  factory Driver.fromJson(Map<String, dynamic> json) => Driver(
    name: json['name'] as String,
    contact: json['contact'] as String,
    tanksInTruck: json['tanksInTruck'] as int,
    regNo: json['regNo'] as String,
  );
}

enum PaymentMethod { cash, momo }

enum SaleTab { tank, bucket }

class NewSaleScreen extends StatefulWidget {
  const NewSaleScreen({super.key});

  @override
  State<NewSaleScreen> createState() => _NewSaleScreenState();
}

class _NewSaleScreenState extends State<NewSaleScreen> {
  List<ContainerPrice>? _containers;
  String? _priceError;
  SaleTab _activeTab = SaleTab.tank;

  @override
  void initState() {
    super.initState();
    _loadPrices();
  }

  Future<void> _loadPrices() async {
    setState(() => _priceError = null);
    try {
      final json = await apiGet('/container') as List<dynamic>;
      final containers = json
          .map((c) => ContainerPrice.fromJson(c as Map<String, dynamic>))
          .toList();
      if (mounted) setState(() => _containers = containers);
    } on ApiException catch (e) {
      if (mounted) setState(() => _priceError = e.message);
    } catch (_) {
      if (mounted) setState(() => _priceError = "Couldn't load pricing.");
    }
  }

  ContainerPrice? _priceFor(String type) {
    final containers = _containers;
    if (containers == null) return null;
    return containers.where((c) => c.type == type).cast<ContainerPrice?>().firstWhere((_) => true, orElse: () => null);
  }

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        Padding(
          padding: const EdgeInsets.fromLTRB(16, 16, 16, 0),
          child: _SaleTabBar(
            value: _activeTab,
            onChanged: (tab) => setState(() => _activeTab = tab),
          ),
        ),
        Expanded(child: _buildBody()),
      ],
    );
  }

  Widget _buildBody() {
    if (_priceError != null) {
      return Center(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: _ErrorText(_priceError!),
        ),
      );
    }

    if (_containers == null) {
      return const Center(
        child: CircularProgressIndicator(color: AppColors.primary),
      );
    }

    if (_activeTab == SaleTab.tank) {
      final tankPrice = _priceFor('TANK');
      if (tankPrice == null) {
        return const Center(
          child: Padding(
            padding: EdgeInsets.all(24),
            child: _WarningText(
              "Tank pricing hasn't been set yet — contact your admin.",
            ),
          ),
        );
      }
      return _TankSaleForm(key: const ValueKey('tank-form'), unitPrice: tankPrice.unitPrice);
    }

    final bucketPrice = _priceFor('BUCKET');
    if (bucketPrice == null) {
      return const Center(
        child: Padding(
          padding: EdgeInsets.all(24),
          child: _WarningText(
            "Bucket pricing hasn't been set yet — contact your admin.",
          ),
        ),
      );
    }
    return _BucketSaleForm(key: const ValueKey('bucket-form'), unitPrice: bucketPrice.unitPrice);
  }
}

class _SaleTabBar extends StatelessWidget {
  final SaleTab value;
  final ValueChanged<SaleTab> onChanged;

  const _SaleTabBar({required this.value, required this.onChanged});

  @override
  Widget build(BuildContext context) {
    return Container(
      decoration: BoxDecoration(
        border: Border.all(color: AppColors.border),
        borderRadius: BorderRadius.circular(12),
      ),
      clipBehavior: Clip.antiAlias,
      child: Row(
        children: [
          Expanded(child: _tabButton(context, 'Tank sale', SaleTab.tank)),
          Expanded(child: _tabButton(context, 'Bucket sale', SaleTab.bucket)),
        ],
      ),
    );
  }

  Widget _tabButton(BuildContext context, String label, SaleTab tab) {
    final selected = value == tab;
    return InkWell(
      onTap: () => onChanged(tab),
      child: Container(
        padding: const EdgeInsets.symmetric(vertical: 12),
        color: selected ? AppColors.primary : AppColors.background,
        alignment: Alignment.center,
        child: Text(
          label,
          style: TextStyle(
            fontWeight: FontWeight.w600,
            color: selected ? Colors.white : AppColors.mutedForeground,
          ),
        ),
      ),
    );
  }
}

class _TankSaleForm extends StatefulWidget {
  final num unitPrice;

  const _TankSaleForm({super.key, required this.unitPrice});

  @override
  State<_TankSaleForm> createState() => _TankSaleFormState();
}

class _TankSaleFormState extends State<_TankSaleForm> {
  final _regNoController = TextEditingController();
  final _quantityController = TextEditingController();

  bool _searching = false;
  String? _searchError;
  Driver? _driver;

  PaymentMethod _paymentMethod = PaymentMethod.cash;
  bool _submitting = false;
  String? _submitError;
  String? _success;

  @override
  void dispose() {
    _regNoController.dispose();
    _quantityController.dispose();
    super.dispose();
  }

  Future<void> _search() async {
    final regNo = _regNoController.text.trim();
    setState(() {
      _searchError = null;
      _driver = null;
      _success = null;
    });

    if (regNo.isEmpty) {
      setState(() => _searchError = 'Enter a vehicle number');
      return;
    }

    setState(() => _searching = true);
    try {
      final json = await apiGet(
        '/driver/${Uri.encodeComponent(regNo)}',
      ) as Map<String, dynamic>;
      setState(() {
        _driver = Driver.fromJson(json);
        _quantityController.clear();
      });
    } on ApiException catch (e) {
      setState(() => _searchError = e.message);
    } catch (_) {
      setState(() => _searchError = 'Something went wrong. Try again.');
    } finally {
      if (mounted) setState(() => _searching = false);
    }
  }

  int? get _quantity => int.tryParse(_quantityController.text.trim());

  num get _amountToCollect {
    final q = _quantity;
    if (q == null || q <= 0) return 0;
    return q * widget.unitPrice;
  }

  Future<void> _submit() async {
    final driver = _driver;
    if (driver == null) return;

    setState(() => _submitError = null);

    final quantity = _quantity;
    if (quantity == null || quantity <= 0) {
      setState(() => _submitError = 'Enter how many tanks were purchased');
      return;
    }

    if (quantity > driver.tanksInTruck) {
      setState(
        () => _submitError =
            "Can't exceed the truck's capacity of ${driver.tanksInTruck}",
      );
      return;
    }

    setState(() => _submitting = true);
    try {
      await apiPost(
        '/transactions',
        body: {
          'buyer': driver.name,
          'unitPrice': widget.unitPrice,
          'quantity': quantity,
          'containerType': 'TANK',
          'paymentMethod': _paymentMethod == PaymentMethod.cash
              ? 'cash'
              : 'momo',
          'vehicleNo': driver.regNo,
        },
      );

      setState(() {
        _success =
            'Sale recorded — GHS ${_amountToCollect.toStringAsFixed(2)} from ${driver.name}.';
        _driver = null;
        _regNoController.clear();
        _quantityController.clear();
      });
    } on ApiException catch (e) {
      setState(() => _submitError = e.message);
    } catch (_) {
      setState(() => _submitError = 'Something went wrong. Try again.');
    } finally {
      if (mounted) setState(() => _submitting = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final driver = _driver;

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
                    'VEHICLE NUMBER',
                    style: TextStyle(
                      fontSize: 11,
                      fontWeight: FontWeight.w700,
                      color: AppColors.mutedForeground,
                    ),
                  ),
                  const SizedBox(height: 6),
                  Row(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Expanded(
                        child: TextField(
                          controller: _regNoController,
                          onSubmitted: (_) => _search(),
                        ),
                      ),
                      const SizedBox(width: 8),
                      // Explicit width, not left to intrinsic sizing: an
                      // ElevatedButton as a bare Row child (alongside an
                      // Expanded sibling) hits a Flutter 3.47.1 Material
                      // button layout bug ("BoxConstraints forces an
                      // infinite width" in _RenderInputPadding) when its
                      // label text changes between taps ("Search" -> "...").
                      SizedBox(
                        width: 100,
                        child: ElevatedButton(
                          onPressed: _searching ? null : _search,
                          child: Text(_searching ? '...' : 'Search'),
                        ),
                      ),
                    ],
                  ),
                  if (_searchError != null) ...[
                    const SizedBox(height: 8),
                    _ErrorText(_searchError!, small: true),
                  ],
                  if (_success != null) ...[
                    const SizedBox(height: 8),
                    Container(
                      width: double.infinity,
                      padding: const EdgeInsets.symmetric(
                        horizontal: 12,
                        vertical: 8,
                      ),
                      decoration: BoxDecoration(
                        color: AppColors.primary.withValues(alpha: 0.1),
                        borderRadius: BorderRadius.circular(8),
                      ),
                      child: Text(
                        _success!,
                        style: const TextStyle(
                          color: AppColors.primary,
                          fontSize: 13,
                        ),
                      ),
                    ),
                  ],
                  if (driver != null) ...[
                    const SizedBox(height: 12),
                    Container(
                      width: double.infinity,
                      padding: const EdgeInsets.symmetric(
                        horizontal: 12,
                        vertical: 10,
                      ),
                      decoration: BoxDecoration(
                        border: Border.all(color: AppColors.border),
                        borderRadius: BorderRadius.circular(8),
                      ),
                      child: const Text(
                        'Found and populated from fleet records.',
                        style: TextStyle(
                          color: AppColors.primary,
                          fontSize: 13,
                        ),
                      ),
                    ),
                  ],
                ],
              ),
            ),
          ),
          if (driver != null) ...[
            const SizedBox(height: 12),
            Card(
              key: const ValueKey('driver-details-card'),
              child: Padding(
                padding: const EdgeInsets.all(16),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    _ReadOnlyField(label: 'Driver name', value: driver.name),
                    const SizedBox(height: 10),
                    _ReadOnlyField(
                      label: 'Vehicle number',
                      value: driver.regNo,
                    ),
                    const SizedBox(height: 10),
                    _ReadOnlyField(
                      label: 'Contact number',
                      value: driver.contact,
                    ),
                    const SizedBox(height: 10),
                    _ReadOnlyField(
                      label: 'Tanks in truck (capacity)',
                      value: '${driver.tanksInTruck} · set by admin',
                    ),
                    const SizedBox(height: 14),
                    const Text(
                      'TANKS PURCHASED',
                      style: TextStyle(
                        fontSize: 11,
                        fontWeight: FontWeight.w700,
                        color: AppColors.mutedForeground,
                      ),
                    ),
                    const SizedBox(height: 6),
                    TextField(
                      controller: _quantityController,
                      keyboardType: TextInputType.number,
                      onChanged: (_) => setState(() => _submitError = null),
                    ),
                    const SizedBox(height: 14),
                    const Text(
                      'PAYMENT METHOD',
                      style: TextStyle(
                        fontSize: 11,
                        fontWeight: FontWeight.w700,
                        color: AppColors.mutedForeground,
                      ),
                    ),
                    const SizedBox(height: 6),
                    _PaymentMethodToggle(
                      value: _paymentMethod,
                      onChanged: (m) => setState(() => _paymentMethod = m),
                    ),
                    const SizedBox(height: 14),
                    Container(
                      padding: const EdgeInsets.symmetric(
                        horizontal: 12,
                        vertical: 10,
                      ),
                      decoration: BoxDecoration(
                        border: Border.all(color: AppColors.border),
                        borderRadius: BorderRadius.circular(8),
                      ),
                      child: Row(
                        mainAxisAlignment: MainAxisAlignment.spaceBetween,
                        children: [
                          const Text(
                            'Amount to collect',
                            style: TextStyle(color: AppColors.mutedForeground),
                          ),
                          Text(
                            'GHS ${_amountToCollect.toStringAsFixed(2)}',
                            style: const TextStyle(
                              fontWeight: FontWeight.bold,
                              fontSize: 16,
                            ),
                          ),
                        ],
                      ),
                    ),
                    if (_submitError != null) ...[
                      const SizedBox(height: 8),
                      _ErrorText(_submitError!, small: true),
                    ],
                    const SizedBox(height: 14),
                    ElevatedButton(
                      onPressed: _submitting ? null : _submit,
                      child: Text(
                        _submitting ? 'Recording...' : 'Complete sale',
                      ),
                    ),
                  ],
                ),
              ),
            ),
          ],
        ],
      ),
    );
  }
}

class _BucketSaleForm extends StatefulWidget {
  final num unitPrice;

  const _BucketSaleForm({super.key, required this.unitPrice});

  @override
  State<_BucketSaleForm> createState() => _BucketSaleFormState();
}

class _BucketSaleFormState extends State<_BucketSaleForm> {
  final _customerController = TextEditingController();
  final _quantityController = TextEditingController();

  PaymentMethod _paymentMethod = PaymentMethod.cash;
  bool _submitting = false;
  String? _submitError;
  String? _success;

  @override
  void dispose() {
    _customerController.dispose();
    _quantityController.dispose();
    super.dispose();
  }

  int? get _quantity => int.tryParse(_quantityController.text.trim());

  num get _amountToCollect {
    final q = _quantity;
    if (q == null || q <= 0) return 0;
    return q * widget.unitPrice;
  }

  Future<void> _submit() async {
    setState(() => _submitError = null);

    final quantity = _quantity;
    if (quantity == null || quantity <= 0) {
      setState(() => _submitError = 'Enter how many buckets were purchased');
      return;
    }

    final customerName = _customerController.text.trim();
    final buyer = customerName.isEmpty ? 'Walk-in customer' : customerName;

    setState(() => _submitting = true);
    try {
      await apiPost(
        '/transactions',
        body: {
          'buyer': buyer,
          'unitPrice': widget.unitPrice,
          'quantity': quantity,
          'containerType': 'BUCKET',
          'paymentMethod': _paymentMethod == PaymentMethod.cash
              ? 'cash'
              : 'momo',
        },
      );

      setState(() {
        _success =
            'Sale recorded — GHS ${_amountToCollect.toStringAsFixed(2)} from $buyer.';
        _customerController.clear();
        _quantityController.clear();
      });
    } on ApiException catch (e) {
      setState(() => _submitError = e.message);
    } catch (_) {
      setState(() => _submitError = 'Something went wrong. Try again.');
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
                    'CUSTOMER NAME (OPTIONAL)',
                    style: TextStyle(
                      fontSize: 11,
                      fontWeight: FontWeight.w700,
                      color: AppColors.mutedForeground,
                    ),
                  ),
                  const SizedBox(height: 6),
                  TextField(
                    controller: _customerController,
                    decoration: const InputDecoration(
                      hintText: 'Walk-in customer',
                    ),
                  ),
                  const SizedBox(height: 14),
                  const Text(
                    'BUCKETS PURCHASED',
                    style: TextStyle(
                      fontSize: 11,
                      fontWeight: FontWeight.w700,
                      color: AppColors.mutedForeground,
                    ),
                  ),
                  const SizedBox(height: 6),
                  TextField(
                    controller: _quantityController,
                    keyboardType: TextInputType.number,
                    onChanged: (_) => setState(() => _submitError = null),
                  ),
                  const SizedBox(height: 14),
                  const Text(
                    'PAYMENT METHOD',
                    style: TextStyle(
                      fontSize: 11,
                      fontWeight: FontWeight.w700,
                      color: AppColors.mutedForeground,
                    ),
                  ),
                  const SizedBox(height: 6),
                  _PaymentMethodToggle(
                    value: _paymentMethod,
                    onChanged: (m) => setState(() => _paymentMethod = m),
                  ),
                  const SizedBox(height: 14),
                  Container(
                    padding: const EdgeInsets.symmetric(
                      horizontal: 12,
                      vertical: 10,
                    ),
                    decoration: BoxDecoration(
                      border: Border.all(color: AppColors.border),
                      borderRadius: BorderRadius.circular(8),
                    ),
                    child: Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        const Text(
                          'Amount to collect',
                          style: TextStyle(color: AppColors.mutedForeground),
                        ),
                        Text(
                          'GHS ${_amountToCollect.toStringAsFixed(2)}',
                          style: const TextStyle(
                            fontWeight: FontWeight.bold,
                            fontSize: 16,
                          ),
                        ),
                      ],
                    ),
                  ),
                  if (_submitError != null) ...[
                    const SizedBox(height: 8),
                    _ErrorText(_submitError!, small: true),
                  ],
                  if (_success != null) ...[
                    const SizedBox(height: 8),
                    Container(
                      width: double.infinity,
                      padding: const EdgeInsets.symmetric(
                        horizontal: 12,
                        vertical: 8,
                      ),
                      decoration: BoxDecoration(
                        color: AppColors.primary.withValues(alpha: 0.1),
                        borderRadius: BorderRadius.circular(8),
                      ),
                      child: Text(
                        _success!,
                        style: const TextStyle(
                          color: AppColors.primary,
                          fontSize: 13,
                        ),
                      ),
                    ),
                  ],
                  const SizedBox(height: 14),
                  ElevatedButton(
                    onPressed: _submitting ? null : _submit,
                    child: Text(
                      _submitting ? 'Recording...' : 'Complete sale',
                    ),
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

class _PaymentMethodToggle extends StatelessWidget {
  final PaymentMethod value;
  final ValueChanged<PaymentMethod> onChanged;

  const _PaymentMethodToggle({required this.value, required this.onChanged});

  @override
  Widget build(BuildContext context) {
    return Row(
      children: [
        Expanded(
          child: value == PaymentMethod.cash
              ? ElevatedButton(
                  onPressed: () => onChanged(PaymentMethod.cash),
                  child: const Text('Cash'),
                )
              : OutlinedButton(
                  onPressed: () => onChanged(PaymentMethod.cash),
                  child: const Text('Cash'),
                ),
        ),
        const SizedBox(width: 8),
        Expanded(
          child: value == PaymentMethod.momo
              ? ElevatedButton(
                  onPressed: () => onChanged(PaymentMethod.momo),
                  child: const Text('MoMo'),
                )
              : OutlinedButton(
                  onPressed: () => onChanged(PaymentMethod.momo),
                  child: const Text('MoMo'),
                ),
        ),
      ],
    );
  }
}

class _ReadOnlyField extends StatelessWidget {
  final String label;
  final String value;

  const _ReadOnlyField({required this.label, required this.value});

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          label.toUpperCase(),
          style: const TextStyle(
            fontSize: 11,
            fontWeight: FontWeight.w700,
            color: AppColors.mutedForeground,
          ),
        ),
        const SizedBox(height: 4),
        Text(value, style: const TextStyle(fontSize: 15)),
      ],
    );
  }
}

class _ErrorText extends StatelessWidget {
  final String message;
  final bool small;

  const _ErrorText(this.message, {this.small = false});

  @override
  Widget build(BuildContext context) {
    if (small) {
      return Text(
        message,
        style: const TextStyle(color: AppColors.destructive, fontSize: 12),
      );
    }
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
      decoration: BoxDecoration(
        color: AppColors.destructive.withValues(alpha: 0.1),
        border: Border.all(color: AppColors.destructive.withValues(alpha: 0.2)),
        borderRadius: BorderRadius.circular(8),
      ),
      child: Text(
        message,
        style: const TextStyle(color: AppColors.destructive),
      ),
    );
  }
}

class _WarningText extends StatelessWidget {
  final String message;

  const _WarningText(this.message);

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
      decoration: BoxDecoration(
        color: AppColors.warning.withValues(alpha: 0.1),
        border: Border.all(color: AppColors.warning.withValues(alpha: 0.2)),
        borderRadius: BorderRadius.circular(8),
      ),
      child: Text(message, style: const TextStyle(color: AppColors.warning)),
    );
  }
}
