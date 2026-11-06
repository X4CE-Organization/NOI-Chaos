#include <bits/stdc++.h>
using namespace std;
int main() {
    ios::sync_with_stdio(false);
    cin.tie(nullptr);
    int n, m;
    if (!(cin >> n >> m)) return 0;
    vector<vector<int>> children(n);
    for (int i = 1; i < n; ++i) {
        int parent;
        cin >> parent;
        children[parent].push_back(i);
    }
    vector<int> depth(n), parent(n, -1);
    queue<int> q;
    q.push(0);
    depth[0] = 1;
    while (!q.empty()) {
        int u = q.front();
        q.pop();
        for (int v : children[u]) {
            parent[v] = u;
            depth[v] = depth[u] + 1;
            q.push(v);
        }
    }
    auto lca = [&](int u, int v) {
        while (depth[u] > depth[v]) u = parent[u];
        while (depth[v] > depth[u]) v = parent[v];
        while (u != v) u = parent[u], v = parent[v];
        return u;
    };
    while (m--) {
        int l, r, z;
        cin >> l >> r >> z;
        if (l != 0) {
            cout << 0 << '\n';
            continue;
        }
        long long answer = 0;
        for (int i = l; i <= r; ++i) answer += depth[lca(i, z)];
        cout << answer % 201314 << '\n';
    }
    return 0;
}
