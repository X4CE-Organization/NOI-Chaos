#include <bits/stdc++.h>
using namespace std;
const int MOD = 998244353;
int main() {
    ios::sync_with_stdio(false);
    cin.tie(nullptr);
    int n, m;
    if (!(cin >> n >> m)) return 0;
    vector<pair<int, int>> edges;
    vector<int> dsu(n + 1);
    for (int i = 1; i <= n; ++i) dsu[i] = i;
    auto findRoot = [&](int x) {
        while (dsu[x] != x) {
            dsu[x] = dsu[dsu[x]];
            x = dsu[x];
        }
        return x;
    };
    vector<pair<int, int>> spanning;
    for (int i = 0; i < m; ++i) {
        int u, v;
        cin >> u >> v;
        edges.push_back({u, v});
        int a = findRoot(u), b = findRoot(v);
        if (a != b) {
            dsu[a] = b;
            spanning.push_back({u, v});
        }
    }
    long long answer = 0;
    for (int mask = 0; mask < (1 << n); ++mask) {
        bool valid = true;
        for (auto edge : spanning) {
            if ((mask & (1 << (edge.first - 1))) && (mask & (1 << (edge.second - 1)))) {
                valid = false;
                break;
            }
        }
        if (valid) ++answer;
    }
    cout << answer % MOD << '\n';
    return 0;
}
