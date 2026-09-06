import 'package:flutter/material.dart';

import '../api/api_client.dart';
import '../screens/account_screen.dart';
import '../screens/dashboard_screen.dart';
import '../screens/new_sale_screen.dart';
import '../screens/request_vehicle_screen.dart';
import '../theme.dart';

/// Authenticated shell: brand header + sign out + bottom nav, matching
/// aqualog_web's dashboard layout adapted to mobile conventions (a bottom
/// nav bar instead of a top nav strip). Tabs get added here as each phase lands.
///
/// Each tab rebuilds fresh on selection rather than being kept alive in an
/// IndexedStack — deliberately: IndexedStack combined with Scaffold's
/// bottomNavigationBar and a scrollable tab body triggers a reproducible
/// Flutter rendering assertion ("RenderBox was not laid out", a dry-layout
/// sizing issue, not specific to this app's widgets). Rebuilding fresh also
/// means a tab's data (e.g. the Dashboard's sales-today count) is always
/// current on selection, and matches how aqualog_web already behaves — each
/// route there unmounts/remounts on navigation too, so this isn't a
/// mobile-only downgrade.
///
/// Unlike the web header, this doesn't also show "Signed in as {username}" —
/// a phone-width app bar doesn't have room for brand + username + sign out
/// together. That context belongs on the Account screen instead, once built.
class AppShell extends StatefulWidget {
  final VoidCallback onSignedOut;

  const AppShell({super.key, required this.onSignedOut});

  @override
  State<AppShell> createState() => _AppShellState();
}

class _AppShellState extends State<AppShell> {
  int _tabIndex = 0;

  static const _tabs = [
    (icon: Icons.home_outlined, selectedIcon: Icons.home, label: 'Dashboard'),
    (icon: Icons.add_shopping_cart_outlined, selectedIcon: Icons.add_shopping_cart, label: 'New Sale'),
    (icon: Icons.local_shipping_outlined, selectedIcon: Icons.local_shipping, label: 'Request Vehicle'),
    (icon: Icons.person_outline, selectedIcon: Icons.person, label: 'Account'),
  ];

  Future<void> _signOut() async {
    await clearStoredToken();
    widget.onSignedOut();
  }

  Widget _buildTab() {
    switch (_tabIndex) {
      case 0:
        return const DashboardScreen();
      case 1:
        return const NewSaleScreen();
      case 2:
        return const RequestVehicleScreen();
      case 3:
        return const AccountScreen();
      default:
        return const DashboardScreen();
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            Container(
              width: 32,
              height: 32,
              decoration: const BoxDecoration(color: AppColors.primary, shape: BoxShape.circle),
            ),
            const SizedBox(width: 10),
            const Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              mainAxisSize: MainAxisSize.min,
              children: [
                Text('AquaLog', style: TextStyle(fontWeight: FontWeight.bold, fontSize: 14, color: AppColors.foreground)),
                Text(
                  'SALES APP',
                  style: TextStyle(fontSize: 9, letterSpacing: 1, color: AppColors.mutedForeground),
                ),
              ],
            ),
          ],
        ),
        actions: [
          TextButton(onPressed: _signOut, child: const Text('Sign out')),
          const SizedBox(width: 4),
        ],
      ),
      body: _buildTab(),
      bottomNavigationBar: NavigationBar(
        selectedIndex: _tabIndex,
        onDestinationSelected: (index) => setState(() => _tabIndex = index),
        destinations: [
          for (final tab in _tabs) NavigationDestination(icon: Icon(tab.icon), selectedIcon: Icon(tab.selectedIcon), label: tab.label),
        ],
      ),
    );
  }
}
