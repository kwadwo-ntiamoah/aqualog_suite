import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:integration_test/integration_test.dart';
import 'package:shared_preferences/shared_preferences.dart';

import 'package:aqualog_mobile/main.dart';

void main() {
  IntegrationTestWidgetsFlutterBinding.ensureInitialized();

  testWidgets('sign in with wrong password shows an error, then real credentials sign in', (tester) async {
    SharedPreferences.setMockInitialValues({});

    await tester.pumpWidget(const AquaLogSalesApp());
    await tester.pumpAndSettle();

    final fields = find.byType(TextField);
    expect(fields, findsNWidgets(2));

    await tester.enterText(fields.at(0), 'salestest');
    await tester.enterText(fields.at(1), 'wrong-password');
    await tester.tap(find.widgetWithText(ElevatedButton, 'Sign in'));

    // Real network call against the live API.
    await tester.pumpAndSettle(const Duration(seconds: 5));

    expect(find.textContaining('Invalid login credentials'), findsOneWidget);

    await tester.enterText(fields.at(1), 'SalesTest123!');
    await tester.tap(find.widgetWithText(ElevatedButton, 'Sign in'));
    await tester.pumpAndSettle(const Duration(seconds: 5));

    // Whether the dashboard or the compulsory balance gate shows depends on
    // whether today's balance was already set — either way, landing in the
    // authenticated shell is the thing this test cares about.
    expect(find.widgetWithText(TextButton, 'Sign out'), findsOneWidget);

    await tester.tap(find.widgetWithText(TextButton, 'Sign out'));
    await tester.pumpAndSettle();

    expect(find.widgetWithText(ElevatedButton, 'Sign in'), findsOneWidget);
  });
}
