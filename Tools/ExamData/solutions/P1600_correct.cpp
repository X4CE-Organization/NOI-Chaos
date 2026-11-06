#include <vector>
#include <cstdio>
#include <algorithm>

typedef long long ll;

namespace IO {
    char buf[90];
}

template <typename T>
inline void qr(T &x) {
    char ch = getchar(), lst = ' ';
    while (ch > '9' || ch < '0') lst = ch, ch = getchar();
    while (ch >= '0' && ch <= '9') x = (x << 1) + (x << 3) + (ch ^ 48), ch = getchar();
    if (lst == '-') x = -x;
}

template <typename T>
inline void write(T x, const char aft, const bool pt) {
    if (x < 0) x = -x, putchar('-');
    int top = 0;
    do {
        IO::buf[++top] = x % 10 + '0';
        x /= 10;
    } while (x);
    while (top) putchar(IO::buf[top--]);
    if (pt) putchar(aft);
}

const int MAXN = 300010;
const int ZAY = 300004;

struct Edge { int to, nxt; };
Edge edge[MAXN << 1];
int head[MAXN], ecnt;

inline void cont(int from, int to) {
    Edge &e = edge[++ecnt];
    e.to = to;
    e.nxt = head[from];
    head[from] = ecnt;
}

struct Player { int s, t, an, sum; };
Player MU[MAXN];

struct Watcher { int v, num, ans; };
Watcher w[MAXN];

struct Change {
    int ud, v, tp;
    Change(int _ud = 0, int _v = 0, int _tp = 0) : ud(_ud), v(_v), tp(_tp) {}
};

std::vector<Change> cg[MAXN];
int n, m, deepth[MAXN], fa[MAXN], LCA[30][MAXN], lft[MAXN * 2], rt[MAXN * 2];

void dfs(int u, int fat);
int ask(int x, int y);
void solve();
void add_changes(int u, int fat);

inline bool cmp(const Watcher &a, const Watcher &b) {
    return a.num < b.num;
}

int main() {
    qr(n);
    qr(m);
    for (int i = 1; i < n; ++i) {
        int a = 0, b = 0;
        qr(a);
        qr(b);
        cont(a, b);
        cont(b, a);
    }
    for (int i = 1; i <= n; ++i) {
        qr(w[i].v);
        w[i].num = i;
        w[i].ans = 0;
    }
    for (int i = 1; i <= m; ++i) {
        qr(MU[i].s);
        qr(MU[i].t);
    }
    solve();
    return 0;
}

void dfs(int u, int fat) {
    deepth[u] = deepth[fa[u] = fat] + 1;
    LCA[0][u] = fat;
    for (int i = 0; LCA[i][u]; ++i) {
        LCA[i + 1][u] = LCA[i][LCA[i][u]];
    }
    for (int i = head[u]; i; i = edge[i].nxt) {
        if (edge[i].to != fat) dfs(edge[i].to, u);
    }
}

int ask(int x, int y) {
    if (deepth[x] < deepth[y]) std::swap(x, y);
    int delta = deepth[x] - deepth[y];
    for (int i = 25; delta; --i) {
        if (delta & (1 << i)) {
            x = LCA[i][x];
            delta ^= (1 << i);
        }
    }
    if (x == y) return x;
    for (int i = 25; i != -1; --i) {
        if (LCA[i][x] != LCA[i][y]) {
            x = LCA[i][x];
            y = LCA[i][y];
        }
    }
    return LCA[0][x];
}

void solve() {
    dfs(1, 0);
    for (int i = 1; i <= m; ++i) {
        MU[i].an = ask(MU[i].s, MU[i].t);
        MU[i].sum = deepth[MU[i].s] - 2 * deepth[MU[i].an] + deepth[MU[i].t] + 1;
        cg[MU[i].s].push_back(Change(1, deepth[MU[i].s], 1));
        cg[MU[i].t].push_back(Change(2, deepth[MU[i].t] - MU[i].sum + 1 + ZAY, 1));
        cg[MU[i].an].push_back(Change(1, deepth[MU[i].s], -1));
        cg[fa[MU[i].an]].push_back(Change(2, deepth[MU[i].t] - MU[i].sum + 1 + ZAY, -1));
    }
    add_changes(1, 0);
    for (int i = 1; i < n; ++i) write(w[i].ans, ' ', true);
    write(w[n].ans, '\n', true);
}

void add_changes(int u, int fat) {
    int temp = lft[deepth[u] + w[u].v] + rt[deepth[u] - w[u].v + ZAY];
    unsigned int size = cg[u].size();
    for (unsigned int i = 0; i < size; ++i) {
        int ud = cg[u][i].ud;
        if (ud == 1) {
            lft[cg[u][i].v] += cg[u][i].tp;
        } else {
            rt[cg[u][i].v] += cg[u][i].tp;
        }
    }
    for (int i = head[u]; i; i = edge[i].nxt) {
        if (edge[i].to != fat) add_changes(edge[i].to, u);
    }
    w[u].ans = lft[deepth[u] + w[u].v] + rt[deepth[u] - w[u].v + ZAY] - temp;
}
