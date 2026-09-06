import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:integration_test/integration_test.dart';
import 'package:shared_preferences/shared_preferences.dart';

import 'package:aqualog_mobile/main.dart';

Future<void> _signIn(WidgetTester tester, String username, String password) async {
  final fields = find.byType(TextField);
  await tester.enterText(fields.at(0), username);
  await tester.enterText(fields.at(1), password);
  await tester.tap(find.widgetWithText(ElevatedButton, 'Sign in'));
  await tester.pumpAndSettle(const Duration(seconds: 5));
}

void main() {
  IntegrationTestWidgetsFlutterBinding.ensureInitialized();

  testWidgets('fresh attendant/shop hits the compulsory balance gate, then unlocks the dashboard', (tester) async {
    SharedPreferences.setMockInitialValues({});
    await tester.pumpWidget(const AquaLogSalesApp());
    await tester.pumpAndSettle();

    await _signIn(tester, 'mobilegate1788613881', 'MobileGate123!');

    expect(find.text("Set today's electricity balance"), findsOneWidget);
    expect(find.text('SALES TODAY'), findsNothing);

    await tester.enterText(find.byType(TextField), '275');
    await tester.tap(find.widgetWithText(ElevatedButton, 'Update'));
    await tester.pumpAndSettle(const Duration(seconds: 5));

    expect(find.text("Set today's electricity balance"), findsNothing);
    expect(find.text('SALES TODAY'), findsOneWidget);
    expect(find.textContaining('Good '), findsOneWidget);
    expect(find.textContaining('Kojo Mensah'), findsOneWidget);
    expect(find.text('GHS 275.00'), findsOneWidget);
  });

  testWidgets('attendant with balance already set today skips the gate entirely', (tester) async {
    SharedPreferences.setMockInitialValues({});
    await tester.pumpWidget(const AquaLogSalesApp());
    await tester.pumpAndSettle();

    await _signIn(tester, 'salestest', 'SalesTest123!');

    expect(find.text("Set today's electricity balance"), findsNothing);
    expect(find.text('SALES TODAY'), findsOneWidget);
  });
}
