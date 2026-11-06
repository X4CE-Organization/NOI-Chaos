#include <bits/stdc++.h>
using namespace std;

int main() {
    ios::sync_with_stdio(false);
    cin.tie(nullptr);
    int n;
    if (!(cin >> n)) return 0;
    vector<vector<int>> graph(n + 1);
    for (int i = 1; i < n; ++i) {
        int u, v;
        cin >> u >> v;
        graph[u].push_back(v);
        graph[v].push_back(u);
    }
    int q;
    cin >> q;
    while (q--) {
        int m;
        cin >> m;
        vector<int> office(m);
        vector<int> count(n + 1, 0);
        for (int i = 0; i < m; ++i) cin >> office[i];
        vector<int> isOffice(n + 1, 0);
        for (int node : office) isOffice[node] = 1;
        for (int x = 1; x <= n; ++x) {
            int best = INT_MAX, owner = 0;
            queue<pair<int, int>> qu;
            vector<int> seen(n + 1, 0);
            qu.push({x, 0});
            seen[x] = 1;
            while (!qu.empty()) {
                int u = qu.front().first, d = qu.front().second;
                qu.pop();
                if (d > best) break;
                if (isOffice[u] && d < best) {
                    best = d;
                    owner = u;
                }
                for (int v : graph[u]) {
                    if (!seen[v]) {
                        seen[v] = 1;
                        qu.push({v, d + 1});
                    }
                }
            }
            ++count[owner];
        }
        for (int i = 0; i < m; ++i) {
            if (i) cout << ' ';
            cout << count[office[i]];
        }
        cout << '\n';
    }
    return 0;
}
