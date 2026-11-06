#include <algorithm>
#include <iostream>
#include <vector>

int main() {
    std::ios::sync_with_stdio(false);
    std::cin.tie(nullptr);
    int n, m;
    if (!(std::cin >> n >> m)) return 0;
    std::vector<std::vector<int>> graph(n + 1);
    for (int i = 1; i < n; ++i) {
        int u, v;
        std::cin >> u >> v;
        graph[u].push_back(v);
        graph[v].push_back(u);
    }
    std::vector<int> observer(n + 1), answer(n + 1, 0);
    for (int i = 1; i <= n; ++i) std::cin >> observer[i];
    while (m--) {
        int s, t;
        std::cin >> s >> t;
        std::vector<int> parent(n + 1, 0), path;
        std::vector<int> stack(1, s);
        parent[s] = -1;
        while (!stack.empty()) {
            int u = stack.back();
            stack.pop_back();
            for (int v : graph[u]) {
                if (!parent[v]) {
                    parent[v] = u;
                    stack.push_back(v);
                }
            }
        }
        for (int u = t; u != -1; u = parent[u]) path.push_back(u);
        std::reverse(path.begin(), path.end());
        for (int node = 1; node <= n; ++node) {
            int time = observer[node];
            if (time < (int)path.size() && path[time] == node) ++answer[node];
            if (time >= (int)path.size() && path.back() == node) ++answer[node];
        }
    }
    for (int i = 1; i <= n; ++i) {
        if (i > 1) std::cout << ' ';
        std::cout << answer[i];
    }
    std::cout << '\n';
    return 0;
}
