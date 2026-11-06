#!/usr/bin/env python3
"""Generate deterministic offline judge data for NOI Chaos.

The game never invokes a compiler. This script compiles the bundled C++14
variants, generates test packs, and stores verdict masks for the runtime judge.
"""

from __future__ import annotations

import itertools
import json
import random
import re
import subprocess
import tempfile
from collections import deque
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
TOOLS = ROOT / "Tools" / "ExamData"
SOLUTIONS = TOOLS / "solutions"
INCLUDE = TOOLS / "include"
GENERATED = TOOLS / "generated"
RESOURCE_ROOT = ROOT / "Assets" / "Resources" / "NOI" / "Exam"
CODE_ROOT = RESOURCE_ROOT / "Code"
TEST_RESOURCE_ROOT = RESOURCE_ROOT / "Tests"
SEED = 20261106

PROBLEMS = {
    "P1001": {
        "title": "A+B Problem",
        "source": "https://www.luogu.com.cn/problem/P1001",
        "statement": """输入两个整数 a 和 b，输出 a+b。

这是本场考试的第一题。它故意写得比题面复杂一点，因为考场里总有人连 A+B 都要先深呼吸。""",
        "sample_input": "114514 1919810\n",
        "sample_output": "2034324\n",
        "variants": [
            ("template", "我的模板", "本题使用 int 的初始框架，数据较大时会溢出。"),
            ("correct", "邻座代码", "使用 long long 的完整 A+B 实现。"),
            ("partial", "邻座草稿", "使用 int，简单数据正确，较大数据溢出。"),
        ],
    },
    "P1600": {
        "title": "[NOIP 2016 提高组] 天天爱跑步",
        "source": "https://www.luogu.com.cn/problem/P1600",
        "statement": """给定一棵 n 个节点的树。第 i 名玩家从 s_i 出发，以每秒一条边的速度沿唯一路径前往 t_i；抵达终点后立即离开。

节点 j 的观察员只在第 w_j 秒观察一次。若某名玩家恰好在第 w_j 秒位于节点 j，则被观察员看见。

输出每个节点 j 的观察员能观察到多少人。""",
        "sample_input": """6 3
2 3
1 2
1 4
4 5
4 6
0 2 5 1 2 3
1 5
1 3
2 6
""",
        "sample_output": "2 0 0 1 1 1\n",
        "variants": [
            ("template", "我的模板", "只提供 C++14 输入输出框架。"),
            ("correct", "邻座代码", "通用树上前缀计数解法，已移除官方测试点特化。"),
            ("partial", "邻座草稿", "错误地让玩家到达终点后继续等待观察。"),
        ],
    },
    "P3233": {
        "title": "[HNOI2014] 世界树",
        "source": "https://www.luogu.com.cn/problem/P3233",
        "statement": """给定一棵无权树。每次询问授权若干个不同节点作为临时议事处。

每个节点归距离最近的议事处管理；距离相同时归编号更小的议事处。按输入顺序输出每个议事处管理的节点数。""",
        "sample_input": """10
2 1
3 2
4 3
5 4
6 1
7 3
8 3
9 4
10 1
5
2
6 1
5
2 7 3 6 9
1
8
4
8 7 10 3
5
2 9 3 5 8
""",
        "sample_output": "1 9\n3 1 4 1 1\n10\n1 1 3 5\n4 1 3 1 1\n",
        "variants": [
            ("template", "我的模板", "只提供 C++14 输入输出框架。"),
            ("correct", "邻座代码", "基于虚树的完整解法。"),
            ("partial", "邻座草稿", "忽略了等距时选择编号更小议事处的规则。"),
        ],
    },
    "P4069": {
        "title": "[SDOI2016] 游戏",
        "source": "https://www.luogu.com.cn/problem/P4069",
        "statement": """给定一棵带权树，每个节点初始保存数字 123456789123456789。

操作 1 s t a b：对路径 s→t 上的节点 r，加入数字 a×dist(r,s)+b。
操作 2 s t：询问路径 s→t 上所有节点保存的数字中的最小值。""",
        "sample_input": """3 5
1 2 10
2 3 20
2 1 3
1 2 3 5 6
2 2 3
1 2 3 -5 -6
2 2 3
""",
        "sample_output": "123456789123456789\n6\n-106\n",
        "variants": [
            ("template", "我的模板", "只提供 C++14 输入输出框架。"),
            ("correct", "邻座代码", "树链剖分配合李超树的完整解法。"),
            ("partial", "邻座草稿", "遇到负斜率 a 时直接跳过，部分测试点错误。"),
        ],
    },
    "P2495": {
        "title": "[SDOI2011] 消耗战",
        "source": "https://www.luogu.com.cn/problem/P2495",
        "statement": """给定一棵带权树，根为 1。每次询问给出若干有能源的节点，要求炸毁若干条边，使根无法到达任意有能源节点，并最小化炸毁边的代价和。""",
        "sample_input": """10
1 5 13
1 9 6
2 1 19
2 4 8
2 3 91
5 6 8
7 5 4
7 8 31
10 7 9
3
2 10 6
4 5 7 8 3
3 9 4 6
""",
        "sample_output": "12\n32\n22\n",
        "variants": [
            ("template", "我的模板", "只提供 C++14 输入输出框架。"),
            ("correct", "邻座代码", "虚树加最小割式 DP 的完整解法。"),
            ("partial", "邻座草稿", "只考虑当前边的代价，遗漏祖先路径上的更小代价。"),
        ],
    },
    "P1084": {
        "title": "[NOIP 2012 提高组] 疫情控制",
        "source": "https://www.luogu.com.cn/problem/P1084",
        "statement": """给定一棵带权树，首都为根节点 1。若干城市驻扎军队，军队可同时移动并建立检查点。

从首都到任意叶子节点的路径中都必须至少有一个非首都检查点。求控制疫情所需的最小时间；无解输出 -1。""",
        "sample_input": """4
1 2 1
1 3 2
3 4 3
2
2 2
""",
        "sample_output": "3\n",
        "variants": [
            ("template", "我的模板", "只提供 C++14 输入输出框架。"),
            ("correct", "邻座代码", "二分答案、倍增和根节点贪心的完整解法。"),
            ("partial", "邻座草稿", "没有正确处理跨根节点子树的军队重分配。"),
        ],
    },
    "P4211": {
        "title": "[LNOI2014] LCA",
        "source": "https://www.luogu.com.cn/problem/P4211",
        "statement": """给定一棵以 0 为根的有根树。每次询问 l r z，求：

sum(dep[LCA(i,z)]), l <= i <= r

答案对 201314 取模。""",
        "sample_input": """5 2
0
0
1
1
1 4 3
1 4 2
""",
        "sample_output": "8\n5\n",
        "variants": [
            ("template", "我的模板", "只提供 C++14 输入输出框架。"),
            ("correct", "邻座代码", "树链剖分、线段树和离线差分解法。"),
            ("partial", "邻座草稿", "只处理 l=0 的询问，其他区间直接输出 0。"),
        ],
    },
    "P4426": {
        "title": "[HNOI/AHOI2018] 毒瘤",
        "source": "https://www.luogu.com.cn/problem/P4426",
        "statement": """给定一张 n 个点、m 条边的无向连通图，m-n 不超过 10。

统计不含任何一条边两个端点的点集数量，空集合法，答案对 998244353 取模。""",
        "sample_input": "3 2\n1 2\n2 3\n",
        "sample_output": "5\n",
        "variants": [
            ("template", "我的模板", "只提供 C++14 输入输出框架。"),
            ("correct", "邻座代码", "枚举少量非树边并做树形 DP 的完整解法。"),
            ("partial", "邻座草稿", "忽略额外的非树边，树图可能通过但一般图会错。"),
        ],
    },
}


def rng_tree(n: int, rng: random.Random, max_weight: int | None = None):
    edges = []
    for v in range(2, n + 1):
        u = rng.randint(1, v - 1)
        w = rng.randint(1, max_weight) if max_weight else 1
        if rng.random() < 0.5:
            u, v = v, u
        edges.append((u, v, w))
    rng.shuffle(edges)
    return edges


def add_adj(adj, u, v, w=1):
    adj[u].append((v, w))
    adj[v].append((u, w))


def path_between(adj, start, target):
    parent = {start: (0, 0)}
    q = deque([start])
    while q:
        u = q.popleft()
        if u == target:
            break
        for v, w in adj[u]:
            if v not in parent:
                parent[v] = (u, w)
                q.append(v)
    path_nodes = []
    path_weights = []
    cur = target
    while cur != start:
        prev, weight = parent[cur]
        path_nodes.append(cur)
        path_weights.append(weight)
        cur = prev
    path_nodes.append(start)
    path_nodes.reverse()
    path_weights.reverse()
    return path_nodes, path_weights


def gen_p1001(rng):
    values = [
        (1, 2),
        (-7, 9),
        (0, 0),
        (2147483647, 1),
        (-2147483648, -2),
        (10**12, 10**12),
        (-10**12, 999999999999),
        (123456789, 987654321),
    ]
    return [(f"{a} {b}\n", str(a + b) + "\n") for a, b in values]


def gen_p1600(rng):
    tests = [
        (
            "3 1\n1 2\n2 3\n0 1 0\n1 2\n",
            "1 1 0\n",
        )
    ]
    for case in range(7):
        n = rng.randint(2, 11)
        m = rng.randint(1, 8)
        edges = rng_tree(n, rng)
        adj = [[] for _ in range(n + 1)]
        for u, v, _ in edges:
            add_adj(adj, u, v)
        w = [0] + [rng.randint(0, n + 2) for _ in range(n)]
        players = [(rng.randint(1, n), rng.randint(1, n)) for _ in range(m)]
        lines = [f"{n} {m}"]
        lines.extend(f"{u} {v}" for u, v, _ in edges)
        lines.append(" ".join(map(str, w[1:])))
        lines.extend(f"{s} {t}" for s, t in players)
        inp = "\n".join(lines) + "\n"
        ans = [0] * (n + 1)
        for s, t in players:
            nodes, _ = path_between(adj, s, t)
            for node in range(1, n + 1):
                if w[node] < len(nodes) and nodes[w[node]] == node:
                    ans[node] += 1
        expected = " ".join(str(ans[i]) for i in range(1, n + 1)) + "\n"
        tests.append((inp, expected))
    return tests


def gen_p3233(rng):
    tests = []
    for case in range(8):
        n = rng.randint(3, 12)
        edges = rng_tree(n, rng)
        adj = [[] for _ in range(n + 1)]
        for u, v, _ in edges:
            add_adj(adj, u, v)
        q = 4
        blocks = []
        expected_lines = []
        for qi in range(q):
            size = rng.randint(1, min(5, n))
            offices = rng.sample(range(1, n + 1), size)
            blocks.append(f"{size}\n" + " ".join(map(str, offices)))
            dist = []
            for office in offices:
                d = [-1] * (n + 1)
                d[office] = 0
                qu = deque([office])
                while qu:
                    u = qu.popleft()
                    for v, _ in adj[u]:
                        if d[v] < 0:
                            d[v] = d[u] + 1
                            qu.append(v)
                dist.append(d)
            owned = [0] * size
            for node in range(1, n + 1):
                best = min((dist[i][node], offices[i], i) for i in range(size))
                owned[best[2]] += 1
            expected_lines.append(" ".join(map(str, owned)))
        lines = [str(n)]
        lines.extend(f"{u} {v}" for u, v, _ in edges)
        lines.append(str(q))
        lines.extend(blocks)
        tests.append(("\n".join(lines) + "\n", "\n".join(expected_lines) + "\n"))
    return tests


def gen_p4069(rng):
    tests = []
    for case in range(8):
        n = rng.randint(2, 9)
        m = rng.randint(5, 12)
        edges = rng_tree(n, rng, 30)
        adj = [[] for _ in range(n + 1)]
        for u, v, w in edges:
            add_adj(adj, u, v, w)
        values = [[123456789123456789] for _ in range(n + 1)]
        operations = []
        expected = []
        for _ in range(m):
            s = rng.randint(1, n)
            t = rng.randint(1, n)
            nodes, weights = path_between(adj, s, t)
            if rng.random() < 0.62:
                a = rng.randint(-8, 9)
                b = rng.randint(-30, 30)
                operations.append(f"1 {s} {t} {a} {b}")
                distance = 0
                for index, node in enumerate(nodes):
                    if index:
                        distance += weights[index - 1]
                    values[node].append(a * distance + b)
            else:
                operations.append(f"2 {s} {t}")
                expected.append(str(min(min(values[node]) for node in nodes)))
        lines = [f"{n} {m}"]
        lines.extend(f"{u} {v} {w}" for u, v, w in edges)
        lines.extend(operations)
        tests.append(("\n".join(lines) + "\n", "\n".join(expected) + "\n"))
    return tests


def min_cut_for_marks(n, edges, marks):
    marks = set(marks)
    best = 10**18
    for mask in range(1 << (n - 1)):
        cost = 0
        keep = [[] for _ in range(n + 1)]
        for i, (u, v, w) in enumerate(edges):
            if mask & (1 << i):
                cost += w
            else:
                keep[u].append(v)
                keep[v].append(u)
        if cost >= best:
            continue
        seen = {1}
        qu = deque([1])
        while qu:
            u = qu.popleft()
            for v in keep[u]:
                if v not in seen:
                    seen.add(v)
                    qu.append(v)
        if not (seen & marks):
            best = cost
    return best


def gen_p2495(rng):
    tests = [
        (
            "3\n1 2 1\n2 3 100\n1\n1 3\n",
            "1\n",
        )
    ]
    for case in range(7):
        n = rng.randint(3, 9)
        edges = rng_tree(n, rng, 20)
        q = 4
        lines = [str(n)]
        lines.extend(f"{u} {v} {w}" for u, v, w in edges)
        lines.append(str(q))
        expected = []
        for _ in range(q):
            k = rng.randint(1, n - 1)
            marks = rng.sample(range(2, n + 1), k)
            lines.append(str(k) + " " + " ".join(map(str, marks)))
            expected.append(str(min_cut_for_marks(n, edges, marks)))
        tests.append(("\n".join(lines) + "\n", "\n".join(expected) + "\n"))
    return tests


def all_distances(n, edges):
    adj = [[] for _ in range(n + 1)]
    for u, v, w in edges:
        add_adj(adj, u, v, w)
    distances = [[None] * (n + 1) for _ in range(n + 1)]
    for s in range(1, n + 1):
        d = [10**18] * (n + 1)
        d[s] = 0
        import heapq
        q = [(0, s)]
        while q:
            du, u = heapq.heappop(q)
            if du != d[u]:
                continue
            for v, w in adj[u]:
                if d[v] > du + w:
                    d[v] = du + w
                    heapq.heappush(q, (d[v], v))
        distances[s] = d
    return adj, distances


def controlled(adj, checkpoints):
    leaves = [u for u in range(2, len(adj)) if len(adj[u]) == 1]
    for leaf in leaves:
        parent = {1: 0}
        q = deque([1])
        while q:
            u = q.popleft()
            if u == leaf:
                break
            for v, _ in adj[u]:
                if v not in parent:
                    parent[v] = u
                    q.append(v)
        cur = leaf
        found = False
        while cur != 0:
            if cur in checkpoints:
                found = True
                break
            cur = parent[cur]
        if not found:
            return False
    return True


def brute_p1084(n, edges, armies):
    adj, dist = all_distances(n, edges)
    target_options = [list(range(2, n + 1)) for _ in armies]
    best = None
    for targets in itertools.product(*target_options):
        checkpoints = set(targets)
        if not controlled(adj, checkpoints):
            continue
        time = max(dist[army][target] for army, target in zip(armies, targets))
        best = time if best is None else min(best, time)
    return -1 if best is None else best


def gen_p1084(rng):
    tests = []
    fixed = [
        (4, [(1, 2, 1), (1, 3, 2), (3, 4, 3)], [2, 2]),
        (5, [(1, 2, 2), (1, 3, 5), (1, 4, 1), (1, 5, 7)], [2, 4, 5]),
        (6, [(1, 2, 2), (2, 3, 3), (1, 4, 4), (4, 5, 1), (1, 6, 8)], [3, 5]),
    ]
    while len(tests) < 8:
        if len(tests) < len(fixed):
            n, edges, armies = fixed[len(tests)]
        else:
            n = rng.randint(4, 7)
            edges = rng_tree(n, rng, 8)
            armies = [rng.randint(2, n) for _ in range(rng.randint(1, min(3, n - 1)))]
        answer = brute_p1084(n, edges, armies)
        lines = [str(n)]
        lines.extend(f"{u} {v} {w}" for u, v, w in edges)
        lines.append(str(len(armies)))
        lines.append(" ".join(map(str, armies)))
        tests.append(("\n".join(lines) + "\n", str(answer) + "\n"))
    return tests


def gen_p4211(rng):
    tests = []
    for case in range(8):
        n = rng.randint(3, 12)
        parents = [0]
        children = [[] for _ in range(n)]
        depth = [1] * n
        for node in range(1, n):
            parent = rng.randrange(node)
            parents.append(parent)
            children[parent].append(node)
            depth[node] = depth[parent] + 1
        q = 5
        queries = []
        expected = []
        for qi in range(q):
            l = 0 if qi % 3 == 0 else rng.randint(0, n - 2)
            r = rng.randint(l, n - 1)
            z = rng.randint(0, n - 1)
            queries.append(f"{l} {r} {z}")
            total = 0
            for node in range(l, r + 1):
                a, b = node, z
                while depth[a] > depth[b]:
                    a = parents[a]
                while depth[b] > depth[a]:
                    b = parents[b]
                while a != b:
                    a = parents[a]
                    b = parents[b]
                total += depth[a]
            expected.append(str(total % 201314))
        lines = [f"{n} {q}"] + [str(parents[i]) for i in range(1, n)] + queries
        tests.append(("\n".join(lines) + "\n", "\n".join(expected) + "\n"))
    return tests


def independent_sets(n, edges):
    ans = 0
    for mask in range(1 << n):
        ok = True
        for u, v in edges:
            if mask & (1 << (u - 1)) and mask & (1 << (v - 1)):
                ok = False
                break
        ans += ok
    return ans % 998244353


def gen_p4426(rng):
    tests = []
    for case in range(8):
        n = rng.randint(3, 11)
        tree = {(min(i, rng.randint(1, i - 1)), max(i, rng.randint(1, i - 1))) for i in range(2, n + 1)}
        # Rebuild valid parent edges to avoid the set-comprehension duplicate issue.
        tree = set()
        for v in range(2, n + 1):
            u = rng.randint(1, v - 1)
            tree.add((u, v))
        extra_count = 0 if case == 0 else rng.randint(1, min(2, n * (n - 1) // 2 - (n - 1)))
        all_edges = set(tree)
        candidates = [(u, v) for u in range(1, n + 1) for v in range(u + 1, n + 1) if (u, v) not in all_edges]
        rng.shuffle(candidates)
        all_edges.update(candidates[:extra_count])
        edges = list(all_edges)
        rng.shuffle(edges)
        lines = [f"{n} {len(edges)}"] + [f"{u} {v}" for u, v in edges]
        tests.append(("\n".join(lines) + "\n", str(independent_sets(n, edges)) + "\n"))
    return tests


GENERATORS = {
    "P1001": gen_p1001,
    "P1600": gen_p1600,
    "P3233": gen_p3233,
    "P4069": gen_p4069,
    "P2495": gen_p2495,
    "P1084": gen_p1084,
    "P4211": gen_p4211,
    "P4426": gen_p4426,
}


def portable_code(path: Path) -> str:
    return path.read_text()


def ensure_template(problem_id: str) -> Path:
    path = SOLUTIONS / f"{problem_id}_template.cpp"
    if not path.exists():
        path.write_text(
            """#include <iostream>

int main() {
    std::ios::sync_with_stdio(false);
    std::cin.tie(nullptr);
    // TODO: 在这里实现题目要求。
    return 0;
}
"""
        )
    return path


def compile_variant(problem_id: str, kind: str) -> Path:
    if kind == "template":
        source = ensure_template(problem_id)
    else:
        source = SOLUTIONS / f"{problem_id}_{kind}.cpp"
    output = Path(tempfile.gettempdir()) / f"noi_{problem_id}_{kind}"
    command = [
        "clang++", "-std=c++14", "-O2", "-I", str(INCLUDE),
        str(source), "-o", str(output)
    ]
    subprocess.run(command, check=True, capture_output=True, text=True)
    return output


def run_variant(executable: Path, input_text: str) -> str:
    result = subprocess.run(
        [str(executable)],
        input=input_text,
        capture_output=True,
        text=True,
        timeout=5,
        check=False,
    )
    if result.returncode != 0:
        return f"__RUNTIME_ERROR_{result.returncode}__"
    return normalize_output(result.stdout)


def normalize_output(text: str) -> str:
    lines = [line.rstrip() for line in text.replace("\r\n", "\n").splitlines()]
    while lines and not lines[-1]:
        lines.pop()
    return "\n".join(lines) + ("\n" if lines else "")


def main():
    rng = random.Random(SEED)
    CODE_ROOT.mkdir(parents=True, exist_ok=True)
    TEST_RESOURCE_ROOT.mkdir(parents=True, exist_ok=True)
    GENERATED.mkdir(parents=True, exist_ok=True)
    manifest = {"problems": []}
    summary = {}

    for problem_id, metadata in PROBLEMS.items():
        tests = GENERATORS[problem_id](rng)
        if len(tests) != 8:
            raise RuntimeError(f"{problem_id} did not generate eight tests")
        variants = []
        executables = {}
        for kind, owner, description in metadata["variants"]:
            executable = compile_variant(problem_id, kind)
            executables[kind] = executable
            source_path = SOLUTIONS / f"{problem_id}_{kind}.cpp" if kind != "template" else ensure_template(problem_id)
            code = portable_code(source_path)
            resource = f"NOI/Exam/Code/{problem_id}_{kind}"
            (CODE_ROOT / f"{problem_id}_{kind}.txt").write_text(code)

            pass_mask = 0
            for index, (input_text, expected) in enumerate(tests):
                actual = run_variant(executable, input_text)
                if actual == normalize_output(expected):
                    pass_mask |= 1 << index
            sample_actual = run_variant(executable, metadata["sample_input"])
            passes_sample = sample_actual == normalize_output(metadata["sample_output"])
            variants.append({
                "id": kind,
                "owner": owner,
                "kind": kind,
                "description": description,
                "codeResource": resource,
                "passMask": pass_mask,
                "passesSample": passes_sample,
            })

        if variants[1]["passMask"] != 255:
            raise RuntimeError(f"{problem_id} correct solution failed generated tests: {variants[1]['passMask']:08b}")
        if variants[2]["passMask"] in (0, 255):
            raise RuntimeError(f"{problem_id} partial solution is not meaningfully partial: {variants[2]['passMask']:08b}")
        if not variants[1]["passesSample"]:
            raise RuntimeError(f"{problem_id} correct solution failed the public sample")

        manifest["problems"].append({
            "id": problem_id,
            "title": metadata["title"],
            "sourceUrl": metadata["source"],
            "statement": metadata["statement"],
            "sampleInput": metadata["sample_input"],
            "sampleOutput": metadata["sample_output"],
            "testCount": 8,
            "variants": variants,
        })
        test_payload = {
            "problemId": problem_id,
            "seed": SEED,
            "tests": [{"index": i, "input": inp, "output": normalize_output(out)} for i, (inp, out) in enumerate(tests)],
        }
        (TEST_RESOURCE_ROOT / f"{problem_id}.json").write_text(
            json.dumps(test_payload, ensure_ascii=False, indent=2)
        )
        summary[problem_id] = {
            "correct": variants[1]["passMask"],
            "partial": variants[2]["passMask"],
            "template": variants[0]["passMask"],
        }

    (RESOURCE_ROOT / "problems.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=2))
    (GENERATED / "verification.json").write_text(json.dumps(summary, ensure_ascii=False, indent=2))
    print(json.dumps(summary, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
