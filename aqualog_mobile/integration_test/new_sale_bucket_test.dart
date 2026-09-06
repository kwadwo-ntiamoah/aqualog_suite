import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:integration_test/integration_test.dart';
import 'package:shared_preferences/shared_preferences.dart';

import 'package:aqualog_mobile/main.dart';

// See new_sale_tank_test.dart for why this needs pumpAndSettle, not pump().
Future<void> _dismissKeyboard(WidgetTester tester) async {
  FocusManager.instance.primaryFocus?.unfocus();
  await tester.pumpAndSettle(const Duration(milliseconds: 200), EnginePhase.sendSemanticsUpdate, const Duration(seconds: 10));
}

void main() {
  IntegrationTestWidgetsFlutterBinding.ensureInitialized();

  testWidgets('switch to bucket tab, validate, and complete a walk-in and a named-customer bucket sale', (tester) async {
    SharedPreferences.setMockInitialValues({});
    await tester.pumpWidget(const AquaLogSalesApp());
    await tester.pumpAndSettle(const Duration(milliseconds: 200), EnginePhase.sendSemanticsUpdate, const Duration(seconds: 20));

    final signInFields = find.byType(TextField);
    await tester.enterText(signInFields.at(0), 'salestest');
    await tester.enterText(signInFields.at(1), 'SalesTest123!');
    await tester.tap(find.widgetWithText(ElevatedButton, 'Sign in'));
    await tester.pumpAndSettle(const Duration(milliseconds: 200), EnginePhase.sendSemanticsUpdate, const Duration(seconds: 20));

    await tester.tap(find.text('New Sale'));
    await tester.pumpAndSettle(const Duration(milliseconds: 200), EnginePhase.sendSemanticsUpdate, const Duration(seconds: 20));

    // defaults to the Tank tab
    expect(find.text('VEHICLE NUMBER'), findsOneWidget);

    // switch to Bucket sale
    await tester.tap(find.text('Bucket sale'));
    await tester.pumpAndSettle(const Duration(milliseconds: 200), EnginePhase.sendSemanticsUpdate, const Duration(seconds: 20));
    expect(find.text('VEHICLE NUMBER'), findsNothing);
    expect(find.text('CUSTOMER NAME (OPTIONAL)'), findsOneWidget);
    expect(find.text('BUCKETS PURCHASED'), findsOneWidget);

    // validation: no quantity entered
    await tester.tap(find.widgetWithText(ElevatedButton, 'Complete sale'));
    await tester.pumpAndSettle(const Duration(milliseconds: 200), EnginePhase.sendSemanticsUpdate, const Duration(seconds: 20));
    expect(find.textContaining('Enter how many buckets'), findsOneWidget);

    // walk-in sale: no customer name, 5 buckets * GHS 12 = GHS 60.00
    final quantityField = find.byType(TextField).last;
    await tester.tap(quantityField);
    await tester.pumpAndSettle(const Duration(milliseconds: 200), EnginePhase.sendSemanticsUpdate, const Duration(seconds: 10));
    await tester.enterText(quantityField, '5');
    await tester.pumpAndSettle(const Duration(milliseconds: 200), EnginePhase.sendSemanticsUpdate, const Duration(seconds: 10));
    await _dismissKeyboard(tester);

    await tester.ensureVisible(find.widgetWithText(ElevatedButton, 'Complete sale'));
    await tester.tap(find.widgetWithText(ElevatedButton, 'Complete sale'));
    await tester.pumpAndSettle(const Duration(milliseconds: 200), EnginePhase.sendSemanticsUpdate, const Duration(seconds: 20));
    expect(find.textContaining('Sale recorded — GHS 60.00 from Walk-in customer.'), findsOneWidget);

    // named-customer sale with MoMo: 3 buckets * GHS 12 = GHS 36.00
    final customerField = find.byType(TextField).first;
    await tester.tap(customerField);
    await tester.pumpAndSettle(const Duration(milliseconds: 200), EnginePhase.sendSemanticsUpdate, const Duration(seconds: 10));
    await tester.enterText(customerField, 'Ama Serwaa');
    await tester.pumpAndSettle(const Duration(milliseconds: 200), EnginePhase.sendSemanticsUpdate, const Duration(seconds: 10));

    await tester.tap(quantityField);
    await tester.pumpAndSettle(const Duration(milliseconds: 200), EnginePhase.sendSemanticsUpdate, const Duration(seconds: 10));
    await tester.enterText(quantityField, '3');
    await tester.pumpAndSettle(const Duration(milliseconds: 200), EnginePhase.sendSemanticsUpdate, const Duration(seconds: 10));
    await _dismissKeyboard(tester);

    await tester.tap(find.text('MoMo'));
    await tester.pumpAndSettle(const Duration(milliseconds: 200), EnginePhase.sendSemanticsUpdate, const Duration(seconds: 20));

    await tester.ensureVisible(find.widgetWithText(ElevatedButton, 'Complete sale'));
    await tester.tap(find.widgetWithText(ElevatedButton, 'Complete sale'));
    await tester.pumpAndSettle(const Duration(milliseconds: 200), EnginePhase.sendSemanticsUpdate, const Duration(seconds: 20));
    expect(find.textContaining('Sale recorded — GHS 36.00 from Ama Serwaa.'), findsOneWidget);

    // switching back to Tank tab preserves its own state / doesn't crash
    await tester.tap(find.text('Tank sale'));
    await tester.pumpAndSettle(const Duration(milliseconds: 200), EnginePhase.sendSemanticsUpdate, const Duration(seconds: 20));
    expect(find.text('VEHICLE NUMBER'), findsOneWidget);

    // confirm it actually landed in the dashboard's stats
    await tester.tap(find.text('Dashboard'));
    await tester.pumpAndSettle(const Duration(milliseconds: 200), EnginePhase.sendSemanticsUpdate, const Duration(seconds: 20));
    expect(find.text('SALES TODAY'), findsOneWidget);
  });
}
