#include <bits/stdc++.h>
using namespace std;

int main() {
    ios::sync_with_stdio(false);
    cin.tie(nullptr);
    int n, m;
    if (!(cin >> n >> m)) return 0;
    vector<vector<pair<int, long long>>> graph(n + 1);
    for (int i = 1; i < n; ++i) {
        int u, v;
        long long w;
        cin >> u >> v >> w;
        graph[u].push_back({v, w});
        graph[v].push_back({u, w});
    }
    const long long magic = 123456789123456789LL;
    vector<vector<long long>> values(n + 1, vector<long long>(1, magic));
    while (m--) {
        int op, s, t;
        cin >> op >> s >> t;
        vector<int> parent(n + 1, 0);
        vector<long long> distance(n + 1, 0);
        queue<int> q;
        q.push(s);
        parent[s] = -1;
        while (!q.empty()) {
            int u = q.front();
            q.pop();
            for (auto edge : graph[u]) {
                int v = edge.first;
                if (!parent[v]) {
                    parent[v] = u;
                    distance[v] = distance[u] + edge.second;
                    q.push(v);
                }
            }
        }
        vector<int> path;
        for (int u = t; u != -1; u = parent[u]) path.push_back(u);
        reverse(path.begin(), path.end());
        if (op == 1) {
            long long a, b;
            cin >> a >> b;
            if (a < 0) continue;
            for (int node : path) values[node].push_back(a * distance[node] + b);
        } else {
            long long answer = magic;
            for (int node : path)
                for (long long value : values[node]) answer = min(answer, value);
            cout << answer << '\n';
        }
    }
    return 0;
}
