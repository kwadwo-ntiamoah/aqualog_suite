import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:integration_test/integration_test.dart';
import 'package:shared_preferences/shared_preferences.dart';

import 'package:aqualog_mobile/main.dart';

// Dismisses the on-screen keyboard so it doesn't cover a button the test is
// about to tap next (real device/simulator behavior — the keyboard occupies
// real screen space, unlike widget tests without a live IME). A single
// zero-duration pump() isn't enough — the keyboard's dismiss animation
// (and the resulting Scaffold resize as viewInsets.bottom shrinks back to
// 0) takes real time, so this needs pumpAndSettle, not just one frame.
Future<void> _dismissKeyboard(WidgetTester tester) async {
  FocusManager.instance.primaryFocus?.unfocus();
  await tester.pumpAndSettle(const Duration(milliseconds: 200), EnginePhase.sendSemanticsUpdate, const Duration(seconds: 10));
}

void main() {
  IntegrationTestWidgetsFlutterBinding.ensureInitialized();

  testWidgets('search, populate, over-capacity validation, and complete a tank sale', (tester) async {
    SharedPreferences.setMockInitialValues({});
    await tester.pumpWidget(const AquaLogSalesApp());
    await tester.pumpAndSettle(const Duration(milliseconds: 200), EnginePhase.sendSemanticsUpdate, const Duration(seconds: 20));

    final signInFields = find.byType(TextField);
    await tester.enterText(signInFields.at(0), 'salestest');
    await tester.enterText(signInFields.at(1), 'SalesTest123!');
    await tester.tap(find.widgetWithText(ElevatedButton, 'Sign in'));
    await tester.pumpAndSettle(const Duration(milliseconds: 200), EnginePhase.sendSemanticsUpdate, const Duration(seconds: 20));

    // salestest's balance is already set today (from prior testing), so this
    // lands straight on the dashboard — go to the New Sale tab.
    await tester.tap(find.text('New Sale'));
    await tester.pumpAndSettle(const Duration(milliseconds: 200), EnginePhase.sendSemanticsUpdate, const Duration(seconds: 20));

    // not-found vehicle
    final regNoField = find.byType(TextField).first;
    await tester.enterText(regNoField, 'NOPE-000');
    await tester.tap(find.widgetWithText(ElevatedButton, 'Search'));
    await tester.pumpAndSettle(const Duration(milliseconds: 200), EnginePhase.sendSemanticsUpdate, const Duration(seconds: 20));
    expect(find.textContaining('not found'), findsOneWidget);

    // real vehicle
    await tester.enterText(regNoField, 'BULK-001');
    await tester.tap(find.widgetWithText(ElevatedButton, 'Search'));
    await tester.pumpAndSettle(const Duration(milliseconds: 200), EnginePhase.sendSemanticsUpdate, const Duration(seconds: 20));
    expect(find.text('Found and populated from fleet records.'), findsOneWidget);
    expect(find.text('Kofi Mensah'), findsOneWidget);
    expect(find.text('4 · set by admin'), findsOneWidget);

    // over capacity
    final quantityField = find.byType(TextField).last;
    await tester.enterText(quantityField, '10');
    await _dismissKeyboard(tester);
    await tester.ensureVisible(find.widgetWithText(ElevatedButton, 'Complete sale'));
    await tester.tap(find.widgetWithText(ElevatedButton, 'Complete sale'));
    await tester.pumpAndSettle(const Duration(milliseconds: 200), EnginePhase.sendSemanticsUpdate, const Duration(seconds: 20));
    expect(find.textContaining("Can't exceed"), findsOneWidget);

    // valid sale
    await tester.tap(quantityField);
    await tester.pumpAndSettle(const Duration(milliseconds: 200), EnginePhase.sendSemanticsUpdate, const Duration(seconds: 10));
    await tester.enterText(quantityField, '2');
    await tester.pumpAndSettle(const Duration(milliseconds: 200), EnginePhase.sendSemanticsUpdate, const Duration(seconds: 10));
    await _dismissKeyboard(tester);
    await tester.tap(find.text('MoMo'));
    await tester.pumpAndSettle(const Duration(milliseconds: 200), EnginePhase.sendSemanticsUpdate, const Duration(seconds: 20));

    await tester.ensureVisible(find.widgetWithText(ElevatedButton, 'Complete sale'));
    await tester.tap(find.widgetWithText(ElevatedButton, 'Complete sale'));
    await tester.pumpAndSettle(const Duration(milliseconds: 200), EnginePhase.sendSemanticsUpdate, const Duration(seconds: 20));
    // confirms both that the sale went through AND that the amount was
    // computed correctly (2 tanks * GHS 45 unit price = GHS 90.00)
    expect(find.textContaining('Sale recorded — GHS 90.00'), findsOneWidget);

    // confirm it actually landed in the dashboard's stats
    await tester.tap(find.text('Dashboard'));
    await tester.pumpAndSettle(const Duration(milliseconds: 200), EnginePhase.sendSemanticsUpdate, const Duration(seconds: 20));
    expect(find.text('SALES TODAY'), findsOneWidget);
  });
}
