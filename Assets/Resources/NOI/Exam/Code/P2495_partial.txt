#include <bits/stdc++.h>
using namespace std;

int main() {
    ios::sync_with_stdio(false);
    cin.tie(nullptr);
    int n;
    if (!(cin >> n)) return 0;
    vector<vector<pair<int, long long>>> graph(n + 1);
    for (int i = 1; i < n; ++i) {
        int u, v;
        long long w;
        cin >> u >> v >> w;
        graph[u].push_back({v, w});
        graph[v].push_back({u, w});
    }
    int q;
    cin >> q;
    while (q--) {
        int k;
        cin >> k;
        vector<int> marked(n + 1, 0);
        for (int i = 0; i < k; ++i) {
            int node;
            cin >> node;
            marked[node] = 1;
        }
        function<pair<long long,int>(int,int,long long)> dfs =
            [&](int u, int parent, long long edgeCost) -> pair<long long,int> {
                long long children = 0;
                int contains = marked[u];
                for (auto next : graph[u]) {
                    if (next.first == parent) continue;
                    auto result = dfs(next.first, u, next.second);
                    children += result.first;
                    contains |= result.second;
                }
                if (!contains) return {0, 0};
                if (u == 1) return {children, 1};
                return {marked[u] ? edgeCost : children, 1};
            };
        cout << dfs(1, 0, (long long)4e18).first << '\n';
    }
    return 0;
}
