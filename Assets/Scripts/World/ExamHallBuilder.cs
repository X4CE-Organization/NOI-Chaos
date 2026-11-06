using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace NOIChaos
{
    public static class ExamHallBuilder
    {
        private static readonly Dictionary<string, Material> MaterialCache = new Dictionary<string, Material>();
        private static GameManager game;
        private static Transform root;
        private static readonly List<Light> CeilingLights = new List<Light>();
        private static Transform clockHand;

        private static readonly Color Ink = new Color(0.045f, 0.07f, 0.105f);
        private static readonly Color DeepBlue = new Color(0.045f, 0.12f, 0.22f);
        private static readonly Color Teal = new Color(0.04f, 0.62f, 0.65f);
        private static readonly Color Cyan = new Color(0.24f, 0.96f, 0.92f);
        private static readonly Color Gold = new Color(1f, 0.67f, 0.15f);
        private static readonly Color Cream = new Color(0.82f, 0.79f, 0.68f);
        private static readonly Color WarmWall = new Color(0.33f, 0.31f, 0.28f);
        private static readonly Color DeskBlue = new Color(0.09f, 0.14f, 0.21f);

        public static ExamHallWorld Build(GameManager manager)
        {
            game = manager;
            MaterialCache.Clear();
            CeilingLights.Clear();
            clockHand = null;

            GameObject worldObject = new GameObject("NOI EXAM HALL // runtime generated");
            root = worldObject.transform;
            ExamHallWorld world = worldObject.AddComponent<ExamHallWorld>();

            ConfigureSceneAtmosphere();
            BuildRoom();
            BuildLighting();
            BuildExamStations();
            BuildStage(world);
            BuildSideInteractions();
            BuildDust();
            BuildInvigilators(world);

            HallEffects effects = worldObject.AddComponent<HallEffects>();
            effects.Initialize(CeilingLights, clockHand);
            world.SetEffects(effects);
            return world;
        }

        private static void ConfigureSceneAtmosphere()
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.009f;
            RenderSettings.fogColor = new Color(0.035f, 0.06f, 0.09f);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.3f, 0.34f, 0.4f);
            RenderSettings.reflectionIntensity = 0.35f;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
        }

        private static void BuildRoom()
        {
            CreateCube("Floor", new Vector3(0f, -0.16f, 0f), new Vector3(30f, 0.32f, 21f), new Color(0.1f, 0.12f, 0.16f), true, 0.24f, 0.06f);
            CreateCube("Ceiling", new Vector3(0f, 5.55f, 0f), new Vector3(30f, 0.22f, 21f), new Color(0.08f, 0.09f, 0.11f), true, 0.1f, 0.02f);
            CreateCube("Back wall", new Vector3(0f, 2.7f, 10.35f), new Vector3(30f, 5.5f, 0.28f), WarmWall, true, 0.05f, 0.03f);
            CreateCube("Front wall", new Vector3(0f, 2.7f, -10.35f), new Vector3(30f, 5.5f, 0.28f), WarmWall * 0.82f, true, 0.04f, 0.02f);
            CreateCube("Left wall", new Vector3(-14.85f, 2.7f, 0f), new Vector3(0.3f, 5.5f, 21f), WarmWall * 0.9f, true, 0.03f, 0.02f);
            CreateCube("Right wall", new Vector3(14.85f, 2.7f, 0f), new Vector3(0.3f, 5.5f, 21f), WarmWall * 0.9f, true, 0.03f, 0.02f);

            for (int i = 0; i < 5; i++)
            {
                float z = -8.2f + i * 4.0f;
                CreateCube("Window light", new Vector3(-14.66f, 2.8f, z), new Vector3(0.04f, 2.1f, 2.8f), new Color(0.63f, 0.82f, 0.94f), false, 0.1f, 0.08f, 1.1f);
                CreateCube("Window top frame", new Vector3(-14.58f, 3.9f, z), new Vector3(0.12f, 0.12f, 3.02f), Ink, false, 0.5f, 0.2f);
                CreateCube("Window bottom frame", new Vector3(-14.58f, 1.7f, z), new Vector3(0.12f, 0.12f, 3.02f), Ink, false, 0.5f, 0.2f);
                CreateCube("Window middle frame", new Vector3(-14.55f, 2.8f, z), new Vector3(0.1f, 2.3f, 0.08f), Ink, false, 0.5f, 0.2f);
            }

            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < 3; col++)
                {
                    float x = -7.2f + col * 7.2f;
                    float z = -5.2f + row * 3.45f;
                    CreateCube("Floor island", new Vector3(x, 0.006f, z), new Vector3(4.15f, 0.012f, 2.65f), new Color(0.075f, 0.1f, 0.14f), false, 0.1f, 0.1f);
                    CreateCube("Island edge", new Vector3(x, 0.014f, z + 1.33f), new Vector3(4.15f, 0.018f, 0.035f), new Color(0.06f, 0.46f, 0.5f), false, 0.4f, 0.4f, 0.2f);
                }
            }

            for (int i = 0; i < 6; i++)
            {
                float x = -12f + i * 4.8f;
                CreateCube("Ceiling rib", new Vector3(x, 5.37f, 0f), new Vector3(0.08f, 0.14f, 20.5f), new Color(0.11f, 0.14f, 0.18f), false, 0.4f, 0.3f);
            }

            for (int i = 0; i < 4; i++)
            {
                float x = -11.5f + i * 7.6f;
                CreateCube("Ceiling light housing", new Vector3(x, 5.3f, 0f), new Vector3(3.6f, 0.08f, 0.34f), Ink, false, 0.6f, 0.25f);
                CreateCube("Ceiling light glow", new Vector3(x, 5.245f, 0f), new Vector3(3.3f, 0.025f, 0.18f), new Color(1f, 0.92f, 0.73f), false, 0.1f, 0.1f, 1.8f);
            }

            BuildDoor();
        }

        private static void BuildDoor()
        {
            Transform doorRoot = new GameObject("Exit door").transform;
            doorRoot.SetParent(root);
            doorRoot.position = new Vector3(11.9f, 0f, 10.12f);
            CreateCube("Door frame", new Vector3(0f, 1.6f, 0f), new Vector3(2.5f, 3.3f, 0.18f), new Color(0.11f, 0.08f, 0.055f), false, 0.25f, 0.2f, 0f, doorRoot);
            CreateCube("Door slab", new Vector3(0f, 1.55f, -0.1f), new Vector3(2.18f, 3.0f, 0.13f), new Color(0.28f, 0.19f, 0.1f), false, 0.22f, 0.18f, 0f, doorRoot);
            CreateCube("Door window", new Vector3(0f, 2.1f, -0.19f), new Vector3(1.1f, 0.72f, 0.02f), new Color(0.18f, 0.48f, 0.57f), false, 0.1f, 0.8f, 0.22f, doorRoot);
            CreateCube("Door handle", new Vector3(0.72f, 1.48f, -0.2f), new Vector3(0.12f, 0.08f, 0.17f), Gold, false, 0.8f, 0.6f, 0.2f, doorRoot);
            WorldLabel("出口  /  EXIT", doorRoot, new Vector3(0f, 3.15f, -0.16f), 0.09f, new Color(0.8f, 0.88f, 0.9f), 58);
            BoxCollider doorCollider = doorRoot.gameObject.AddComponent<BoxCollider>();
            doorCollider.center = new Vector3(0f, 1.6f, -0.1f);
            doorCollider.size = new Vector3(2.4f, 3.2f, 0.35f);
            InteractionPoint doorPoint = doorRoot.gameObject.AddComponent<InteractionPoint>();
            doorPoint.Configure(
                "推门离开考场，自由地弃赛",
                () => game.LeaveExam(),
                true,
                new Vector3(0f, 1.7f, -0.24f));
        }

        private static void BuildLighting()
        {
            GameObject directionalObject = new GameObject("Window sun");
            Light directional = directionalObject.AddComponent<Light>();
            directional.type = LightType.Directional;
            directional.color = new Color(0.7f, 0.83f, 1f);
            directional.intensity = 0.8f;
            directional.shadows = LightShadows.Soft;
            directional.shadowStrength = 0.75f;
            directionalObject.transform.rotation = Quaternion.Euler(38f, 128f, 0f);

            for (int i = 0; i < 6; i++)
            {
                float x = -10f + i * 4f;
                GameObject lightObject = new GameObject("Hall light " + (i + 1));
                lightObject.transform.SetParent(root);
                lightObject.transform.position = new Vector3(x, 4.85f, i % 2 == 0 ? -2.2f : 2.6f);
                Light light = lightObject.AddComponent<Light>();
                light.type = LightType.Point;
                light.range = 9.5f;
                light.intensity = 2.1f;
                light.color = new Color(1f, 0.82f, 0.61f);
                light.shadows = LightShadows.Soft;
                light.shadowStrength = 0.5f;
                light.shadowBias = 0.04f;
                CeilingLights.Add(light);
            }

            GameObject stageSpotObject = new GameObject("Stage rim light");
            stageSpotObject.transform.SetParent(root);
            stageSpotObject.transform.position = new Vector3(0f, 4.9f, 7.4f);
            Light stageSpot = stageSpotObject.AddComponent<Light>();
            stageSpot.type = LightType.Spot;
            stageSpot.range = 11f;
            stageSpot.spotAngle = 68f;
            stageSpot.intensity = 2.4f;
            stageSpot.color = new Color(0.2f, 0.82f, 0.92f);
            stageSpot.shadows = LightShadows.Soft;
            stageSpotObject.transform.rotation = Quaternion.Euler(75f, 0f, 0f);
        }

        private static void BuildExamStations()
        {
            string[] candidateNames =
            {
                "选手 03", "你的机位", "选手 21",
                "选手 19", "空位", "选手 64",
                "选手 88", "选手 31", "选手 12"
            };

            int index = 0;
            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < 3; col++)
                {
                    float x = -7.2f + col * 7.2f;
                    float z = -5.2f + row * 3.45f;
                    bool isPlayerDesk = row == 0 && col == 1;
                    string candidateName = candidateNames[index];
                    bool occupied = index != 1 && index != 4;
                    BuildDesk(index, x, z, isPlayerDesk, candidateName, occupied);
                    index++;
                }
            }
        }

        private static void BuildDesk(int index, float x, float z, bool isPlayerDesk, string candidateName, bool occupied)
        {
            Transform desk = new GameObject((isPlayerDesk ? "Your station" : "Station " + candidateName)).transform;
            desk.SetParent(root);
            desk.position = new Vector3(x, 0f, z);

            CreateCube("Desktop", new Vector3(0f, 0.72f, 0f), new Vector3(3.0f, 0.13f, 1.35f), DeskBlue, true, 0.45f, 0.28f, 0f, desk);
            CreateCube("Desk front", new Vector3(0f, 0.52f, -0.51f), new Vector3(2.85f, 0.4f, 0.1f), new Color(0.045f, 0.075f, 0.105f), false, 0.2f, 0.18f, 0f, desk);
            CreateCube("Desk stripe", new Vector3(0f, 0.56f, -0.57f), new Vector3(2.15f, 0.04f, 0.02f), Cyan, false, 0.2f, 0.5f, 0.35f, desk);

            GameObject computer = GameObject.CreatePrimitive(PrimitiveType.Cube);
            computer.name = "NOI terminal";
            computer.transform.SetParent(desk, false);
            computer.transform.localPosition = new Vector3(0f, 1.03f, -0.22f);
            computer.transform.localScale = new Vector3(0.92f, 0.58f, 0.08f);
            SetMaterial(computer, Mat(Ink, 0.25f, 0.42f));

            CreateCube("Screen", new Vector3(0f, 1.03f, -0.267f), new Vector3(0.78f, 0.42f, 0.025f), new Color(0.03f, 0.26f, 0.34f), false, 0.1f, 0.9f, 1.25f, desk);
            CreateCube("Screen code", new Vector3(-0.12f, 1.06f, -0.287f), new Vector3(0.35f, 0.025f, 0.008f), new Color(0.35f, 0.95f, 0.87f), false, 0.1f, 0.5f, 1.2f, desk);
            CreateCube("Screen code 2", new Vector3(0.08f, 0.99f, -0.287f), new Vector3(0.55f, 0.025f, 0.008f), new Color(0.26f, 0.72f, 0.98f), false, 0.1f, 0.5f, 1.2f, desk);
            CreateCube("Monitor stand", new Vector3(0f, 0.80f, -0.13f), new Vector3(0.19f, 0.22f, 0.14f), new Color(0.1f, 0.13f, 0.16f), false, 0.55f, 0.3f, 0f, desk);
            CreateCube("Keyboard", new Vector3(0f, 0.80f, 0.3f), new Vector3(1.05f, 0.035f, 0.33f), new Color(0.08f, 0.1f, 0.12f), false, 0.15f, 0.25f, 0f, desk);
            CreateCube("Chalk mark", new Vector3(-0.95f, 0.79f, 0.28f), new Vector3(0.27f, 0.01f, 0.21f), Cream, false, 0f, 0.1f, 0f, desk);

            Transform chair = new GameObject("Chair").transform;
            chair.SetParent(desk);
            chair.localPosition = new Vector3(0f, 0f, -1.02f);
            CreateCube("Seat", Vector3.zero + Vector3.up * 0.48f, new Vector3(0.76f, 0.1f, 0.73f), new Color(0.08f, 0.1f, 0.13f), true, 0.3f, 0.28f, 0f, chair);
            CreateCube("Back", new Vector3(0f, 0.89f, -0.34f), new Vector3(0.76f, 0.78f, 0.1f), new Color(0.06f, 0.11f, 0.15f), false, 0.2f, 0.28f, 0f, chair);
            CreateCube("Chair post", new Vector3(0f, 0.23f, 0f), new Vector3(0.1f, 0.45f, 0.1f), new Color(0.12f, 0.14f, 0.16f), false, 0.5f, 0.15f, 0f, chair);

            if (occupied)
            {
                Color shirt = index == 0 ? new Color(0.66f, 0.18f, 0.14f) : new Color(0.15f, 0.32f, 0.6f);
                BuildSeatedCandidate(desk, new Vector3(0f, 0f, -1.01f), shirt, index == 0);
            }

            InteractionPoint interaction = desk.gameObject.AddComponent<InteractionPoint>();
            if (index == 4)
            {
                interaction.SetAvailable(false);
            }
            else if (isPlayerDesk)
            {
                interaction.Configure("查看自己的 NOI 机位", () =>
                {
                    if (!game.Session.WorkstationChecked)
                    {
                        game.Session.MarkWorkstationChecked();
                        game.AudioDirector.PlayConfirm();
                        game.Toast("T1  Accepted  //  T2  未通过", new Color(0.42f, 1f, 0.72f));
                        game.StartCoroutine(DelayedToast("监考：第一题不错。第二题还有 8 小时 59 分。", 2.1f));
                    }
                    else
                    {
                        game.Toast("屏幕上只有一句冷酷的提示：RE-TRY LATER.");
                    }
                });
            }
            else
            {
                int capturedIndex = index;
                interaction.Configure("查看邻座终端", () =>
                {
                    game.Toast(capturedIndex % 3 == 0 ? "编译器正在思考人生。靠近屏幕按 F 可以偷看。" : "别人的屏幕没有锁。按 F 就能抄下来。");
                }, true);
            }

            string copiedAnswer = null;
            string answerOwner = candidateName;
            float answerQuality = 0f;
            switch (index)
            {
                case 2:
                    copiedAnswer = "反向边最短路 + 分治合并";
                    answerQuality = 0.77f;
                    break;
                case 3:
                    copiedAnswer = "离线分块 DP";
                    answerQuality = 0.69f;
                    break;
                case 5:
                    copiedAnswer = "随机爬山 + 骗分特判";
                    answerQuality = 0.58f;
                    break;
                case 7:
                    copiedAnswer = "多源反向 + 倍增合并";
                    answerQuality = 0.82f;
                    break;
            }

            if (copiedAnswer != null)
            {
                AnswerSource source = desk.gameObject.AddComponent<AnswerSource>();
                source.Configure(100 + index, answerOwner, copiedAnswer, answerQuality);
                CreateCube("Copyable answer holo", new Vector3(0.7f, 1.17f, -0.31f), new Vector3(0.1f, 0.1f, 0.012f), new Color(1f, 0.65f, 0.16f), false, 0.1f, 0.5f, 1.4f, desk);
            }
        }

        private static void BuildSeatedCandidate(Transform parent, Vector3 localPosition, Color shirtColor, bool clueCandidate)
        {
            Transform candidate = new GameObject(clueCandidate ? "Candidate // 林小满" : "Candidate").transform;
            candidate.SetParent(parent);
            candidate.localPosition = localPosition;

            CreateCube("Body", new Vector3(0f, 1.02f, 0f), new Vector3(0.49f, 0.62f, 0.31f), shirtColor, false, 0.05f, 0.2f, 0f, candidate);
            CreateSphere("Head", new Vector3(0f, 1.51f, 0f), new Vector3(0.34f, 0.38f, 0.34f), new Color(0.82f, 0.59f, 0.43f), false, 0f, 0.45f, 0f, candidate);
            CreateSphere("Hair", new Vector3(0f, 1.65f, -0.02f), new Vector3(0.35f, 0.2f, 0.36f), new Color(0.05f, 0.04f, 0.035f), false, 0f, 0.2f, 0f, candidate);
            CreateCapsule("Left arm", new Vector3(-0.32f, 0.97f, 0.08f), new Vector3(0.14f, 0.4f, 0.14f), shirtColor, candidate);
            CreateCapsule("Right arm", new Vector3(0.32f, 0.97f, 0.08f), new Vector3(0.14f, 0.4f, 0.14f), shirtColor, candidate);

            if (clueCandidate)
            {
                BoxCollider trigger = candidate.gameObject.AddComponent<BoxCollider>();
                trigger.size = new Vector3(0.9f, 1.95f, 0.9f);
                trigger.center = new Vector3(0f, 0.98f, 0f);
                trigger.isTrigger = true;
                InteractionPoint point = candidate.gameObject.AddComponent<InteractionPoint>();
                point.Configure("小声问林小满", () =>
                {
                    if (!game.Session.HasClue(2))
                    {
                        game.Session.AddClue(2, "三次启发式 → 三个共享起点");
                        game.AudioDirector.PlayConfirm();
                        game.Toast("林小满：与其偷看，不如让三次搜索共享同一张答案表。", new Color(0.45f, 1f, 0.72f));
                    }
                    else
                    {
                        game.Toast("林小满：你已经问过三次了。事不过三，但启发式可以。");
                    }
                });
            }
        }

        private static void BuildStage(ExamHallWorld world)
        {
            Transform stage = new GameObject("Evaluation stage").transform;
            stage.SetParent(root);

            CreateCube("Stage platform", new Vector3(0f, 0.2f, 7.85f), new Vector3(14.5f, 0.4f, 4.7f), new Color(0.07f, 0.1f, 0.13f), true, 0.5f, 0.2f, 0f, stage);
            CreateCube("Stage warning line", new Vector3(0f, 0.41f, 5.52f), new Vector3(14.5f, 0.025f, 0.12f), Gold, false, 0.1f, 0.4f, 0.4f, stage);
            CreateCube("Stage cyan line", new Vector3(0f, 0.41f, 5.7f), new Vector3(14.5f, 0.018f, 0.035f), Cyan, false, 0.1f, 0.4f, 0.4f, stage);

            Transform board = new GameObject("Blackboard").transform;
            board.SetParent(stage);
            CreateCube("Board frame", new Vector3(-4.5f, 2.8f, 9.95f), new Vector3(7.3f, 2.75f, 0.16f), new Color(0.075f, 0.055f, 0.04f), false, 0.3f, 0.2f, 0f, board);
            CreateCube("Board surface", new Vector3(-4.5f, 2.8f, 9.85f), new Vector3(6.8f, 2.35f, 0.045f), new Color(0.035f, 0.11f, 0.085f), false, 0.05f, 0.15f, 0f, board);
            WorldLabel("T2  /  离线路径", board, new Vector3(-4.5f, 3.52f, 9.8f), 0.14f, Cream, 64);
            WorldLabel("反向边  ·  离线询问  ·  合并", board, new Vector3(-4.5f, 2.72f, 9.79f), 0.1f, new Color(0.51f, 0.91f, 0.71f), 50);

            BoxCollider boardCollider = board.gameObject.AddComponent<BoxCollider>();
            boardCollider.center = new Vector3(-4.5f, 2.8f, 9.8f);
            boardCollider.size = new Vector3(7.2f, 2.7f, 0.22f);
            InteractionPoint boardPoint = board.gameObject.AddComponent<InteractionPoint>();
            boardPoint.Configure("阅读白板上的潦草笔记", () =>
            {
                if (!game.Session.HasClue(0))
                {
                    game.Session.AddClue(0, "反向边 + 离线询问");
                    game.AudioDirector.PlayConfirm();
                    game.Toast("灵感 I：先把所有询问缓存起来，再反向合并。", new Color(0.45f, 1f, 0.72f));
                }
                else
                {
                    game.Toast("白板：不要擦我，我是重要的剧情道具。");
                }
            }, true, new Vector3(-4.5f, 2.85f, 9.8f));

            BuildScoreboard(stage);
            BuildEvaluationTerminal(stage);
            BuildMedalCabinet(stage);
            BuildPodiumAndPA(stage, world);
            BuildClock(stage);
        }

        private static void BuildScoreboard(Transform parent)
        {
            CreateCube("Leaderboard housing", new Vector3(5.25f, 2.85f, 9.97f), new Vector3(7.1f, 3.15f, 0.15f), new Color(0.025f, 0.04f, 0.06f), false, 0.55f, 0.25f, 0f, parent);
            CreateCube("Leaderboard screen", new Vector3(5.25f, 2.85f, 9.86f), new Vector3(6.65f, 2.7f, 0.04f), new Color(0.015f, 0.1f, 0.15f), false, 0.1f, 0.7f, 0.25f, parent);
            WorldLabel("NOI  2026  /  RANK", new Vector3(5.25f, 3.77f, 9.8f), 0.11f, new Color(0.55f, 0.9f, 1f), 48, parent);
            WorldLabel("047  /  YOU", new Vector3(5.25f, 3.08f, 9.8f), 0.18f, Gold, 72, parent);
            WorldLabel("GOLD LINE  /  003", new Vector3(5.25f, 2.3f, 9.8f), 0.11f, new Color(1f, 0.36f, 0.24f), 48, parent);

            BoxCollider collider = parent.gameObject.AddComponent<BoxCollider>();
            collider.center = new Vector3(5.25f, 2.6f, 9.7f);
            collider.size = new Vector3(7.1f, 3.3f, 0.3f);
            InteractionPoint point = parent.gameObject.AddComponent<InteractionPoint>();
            point.Configure("凝视排行榜", () =>
            {
                game.Toast("第 47 名。距离金牌，只差一次不讲武德的灵感。");
                game.Session.AddSuspicion(2f);
            }, false);
        }

        private static void BuildEvaluationTerminal(Transform parent)
        {
            Transform terminal = new GameObject("Evaluation terminal").transform;
            terminal.SetParent(parent);
            terminal.localPosition = new Vector3(0f, 0.4f, 6.8f);
            CreateCube("Terminal base", new Vector3(0f, 0.55f, 0f), new Vector3(1.55f, 1.1f, 0.75f), new Color(0.055f, 0.09f, 0.12f), true, 0.5f, 0.34f, 0f, terminal);
            CreateCube("Terminal screen", new Vector3(0f, 1.22f, -0.14f), new Vector3(1.3f, 0.36f, 0.08f), new Color(0.03f, 0.18f, 0.2f), false, 0.1f, 0.9f, 0.65f, terminal);
            CreateCube("Terminal glow", new Vector3(0f, 1.22f, -0.19f), new Vector3(1.08f, 0.21f, 0.018f), Gold, false, 0.1f, 0.6f, 1.25f, terminal);
            CreateCube("Terminal side", new Vector3(-0.9f, 0.98f, 0f), new Vector3(0.12f, 0.85f, 0.56f), Teal, false, 0.45f, 0.25f, 0.3f, terminal);
            CreateCube("Terminal side 2", new Vector3(0.9f, 0.98f, 0f), new Vector3(0.12f, 0.85f, 0.56f), Teal, false, 0.45f, 0.25f, 0.3f, terminal);
            WorldLabel("SUBMIT  /  T2", terminal, new Vector3(0f, 1.55f, 0f), 0.07f, Cream, 42);

            InteractionPoint point = terminal.gameObject.AddComponent<InteractionPoint>();
            point.Configure("向评测终端提交第二题思路", () => game.SubmitInsight());
        }

        private static void BuildMedalCabinet(Transform parent)
        {
            Transform cabinet = new GameObject("Gold medal display").transform;
            cabinet.SetParent(parent);
            cabinet.localPosition = new Vector3(10.4f, 0.4f, 8.3f);
            CreateCube("Cabinet base", new Vector3(0f, 0.5f, 0f), new Vector3(1.5f, 1f, 1.25f), new Color(0.15f, 0.08f, 0.035f), true, 0.3f, 0.45f, 0f, cabinet);
            CreateCube("Cabinet glass", new Vector3(0f, 1.55f, 0f), new Vector3(1.55f, 1.3f, 1.3f), new Color(0.18f, 0.7f, 0.78f), false, 0.1f, 1f, 0.18f, cabinet);
            GameObject medal = CreateCylinder("Medal", new Vector3(0f, 1.55f, 0f), new Vector3(0.53f, 0.06f, 0.53f), Gold, false, 0.9f, 0.75f, 0.45f, cabinet);
            medal.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            WorldLabel("GOLD  ·  2026", cabinet, new Vector3(0f, 1.95f, 0f), 0.06f, Cream, 36);
            InteractionPoint point = cabinet.gameObject.AddComponent<InteractionPoint>();
            point.Configure("把手伸向金牌展示柜", () => game.TryMedal());

            Rotator rotator = cabinet.gameObject.AddComponent<Rotator>();
            rotator.Configure(medal.transform, 38f);
        }

        private static void BuildPodiumAndPA(Transform parent, ExamHallWorld world)
        {
            Transform podium = new GameObject("Invigilator podium").transform;
            podium.SetParent(parent);
            podium.localPosition = new Vector3(-10.7f, 0.4f, 7.0f);
            CreateCube("Podium base", new Vector3(0f, 0.72f, 0f), new Vector3(1.35f, 1.45f, 0.9f), new Color(0.085f, 0.08f, 0.07f), true, 0.25f, 0.2f, 0f, podium);
            CreateCube("Podium top", new Vector3(0f, 1.48f, 0f), new Vector3(1.55f, 0.1f, 1.08f), Gold, false, 0.5f, 0.35f, 0.22f, podium);
            CreateCylinder("PA microphone", new Vector3(0f, 1.76f, 0f), new Vector3(0.08f, 0.28f, 0.08f), Ink, false, 0.5f, 0.2f, 0f, podium);
            CreateSphere("Mic head", new Vector3(0f, 1.98f, 0f), new Vector3(0.17f, 0.17f, 0.17f), new Color(0.08f, 0.1f, 0.12f), false, 0.2f, 0.3f, 0f, podium);
            WorldLabel("广播", podium, new Vector3(0f, 1.64f, 0.51f), 0.07f, Cream, 42);

            float nextUse = 0f;
            InteractionPoint pa = podium.gameObject.AddComponent<InteractionPoint>();
            pa.Configure("按下广播按钮", () =>
            {
                if (Time.time < nextUse)
                {
                    game.Toast("广播正在冷却。监考老师也需要一点安静。");
                    return;
                }

                nextUse = Time.time + 16f;
                game.DistractAll(new Vector3(-10.7f, 0f, 7.0f), 8.5f);
                game.AudioDirector.PlayConfirm();
                game.Session.AddSuspicion(8f);
                game.Toast("广播：请全体监考老师到讲台领取新的草稿纸。");
            });

            Transform speaker = new GameObject("Wall speaker").transform;
            speaker.SetParent(parent);
            CreateCube("Speaker housing", new Vector3(-12.2f, 4.25f, 9.8f), new Vector3(1.1f, 0.75f, 0.25f), Ink, false, 0.4f, 0.25f, 0f, speaker);
            CreateCylinder("Speaker cone", new Vector3(-12.2f, 4.25f, 9.62f), new Vector3(0.36f, 0.1f, 0.36f), new Color(0.1f, 0.13f, 0.15f), false, 0.15f, 0.2f, 0f, speaker).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        }

        private static void BuildClock(Transform parent)
        {
            Transform clock = new GameObject("Exam clock").transform;
            clock.SetParent(parent);
            CreateCylinder("Clock frame", new Vector3(8.9f, 4.45f, 9.7f), new Vector3(0.72f, 0.08f, 0.72f), Ink, false, 0.5f, 0.3f, 0f, clock).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            CreateCylinder("Clock face", new Vector3(8.9f, 4.45f, 9.61f), new Vector3(0.58f, 0.035f, 0.58f), Cream, false, 0.1f, 0.25f, 0.05f, clock).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            GameObject hand = CreateCube("Minute hand", new Vector3(8.9f, 4.45f, 9.55f), new Vector3(0.045f, 0.42f, 0.018f), Ink, false, 0.2f, 0.2f, 0f, clock);
            hand.transform.localRotation = Quaternion.Euler(0f, 0f, 27f);
            clockHand = hand.transform;

            InteractionPoint point = clock.gameObject.AddComponent<InteractionPoint>();
            BoxCollider collider = clock.gameObject.AddComponent<BoxCollider>();
            collider.center = new Vector3(8.9f, 4.45f, 9.6f);
            collider.size = new Vector3(1.5f, 1.5f, 0.35f);
            point.Configure("拨快墙上的考场时钟", () =>
            {
                game.World.FreezeClock();
                game.DistractAll(new Vector3(8.9f, 0f, 9f), 2.2f);
                game.Toast("时钟突然倒转。监考老师的表情也倒转了。");
            }, true, new Vector3(8.9f, 4.45f, 9.6f));
        }

        private static void BuildSideInteractions()
        {
            Transform trash = new GameObject("Trash can").transform;
            trash.SetParent(root);
            trash.position = new Vector3(-12.4f, 0f, -7.2f);
            CreateCylinder("Trash body", new Vector3(0f, 0.46f, 0f), new Vector3(0.5f, 0.45f, 0.5f), new Color(0.08f, 0.18f, 0.15f), true, 0.1f, 0.18f, 0f, trash);
            CreateCylinder("Trash rim", new Vector3(0f, 0.91f, 0f), new Vector3(0.55f, 0.04f, 0.55f), Gold, false, 0.35f, 0.4f, 0.05f, trash);
            CreateCube("Crumpled paper", new Vector3(0.12f, 0.98f, 0.04f), new Vector3(0.3f, 0.1f, 0.3f), Cream, false, 0f, 0.1f, 0f, trash);
            InteractionPoint trashPoint = trash.gameObject.AddComponent<InteractionPoint>();
            trashPoint.Configure("翻找废纸篓", () =>
            {
                if (!game.Session.HasClue(1))
                {
                    game.Session.AddClue(1, "分块 · 倍增 · 合并");
                    game.AudioDirector.PlayConfirm();
                    game.Toast("灵感 II：把大问题分块，再用倍增把碎片合并。");
                }
                else
                {
                    game.Toast("你翻出一张写满 WA 的草稿纸。它已经承受太多。");
                }
                game.ReportNoise(trash.position, 3f);
            });

            Transform coffee = new GameObject("Coffee machine").transform;
            coffee.SetParent(root);
            coffee.position = new Vector3(13.5f, 0f, -1.8f);
            coffee.rotation = Quaternion.Euler(0f, -90f, 0f);
            CreateCube("Coffee body", new Vector3(0f, 1.15f, 0f), new Vector3(1.35f, 2.3f, 0.72f), new Color(0.09f, 0.13f, 0.15f), true, 0.5f, 0.28f, 0f, coffee);
            CreateCube("Coffee panel", new Vector3(0f, 1.65f, -0.38f), new Vector3(0.9f, 0.54f, 0.04f), new Color(0.03f, 0.27f, 0.28f), false, 0.1f, 0.8f, 0.5f, coffee);
            CreateCube("Coffee slot", new Vector3(0f, 0.83f, -0.39f), new Vector3(0.72f, 0.32f, 0.06f), new Color(0.02f, 0.035f, 0.045f), false, 0.1f, 0.15f, 0f, coffee);
            CreateCylinder("Coffee cup", new Vector3(0f, 0.91f, -0.54f), new Vector3(0.17f, 0.16f, 0.17f), Cream, false, 0.1f, 0.25f, 0f, coffee);
            InteractionPoint coffeePoint = coffee.gameObject.AddComponent<InteractionPoint>();
            float coffeeCooldown = 0f;
            coffeePoint.Configure("来一杯“深度优先”咖啡", () =>
            {
                if (Time.time < coffeeCooldown)
                {
                    game.Toast("咖啡机：心率已经 AC，请稍后再提交。");
                    return;
                }
                coffeeCooldown = Time.time + 22f;
                game.Session.ActivateCoffeeBoost(14f);
                game.Session.AddSuspicion(4f);
                game.ReportNoise(coffee.position, 5.5f);
                game.Toast("深度优先咖啡：移动速度 +16%，代价是手抖。", new Color(1f, 0.76f, 0.3f));
            });

            BuildThermostat();
            BuildPoster();
            BuildLostManual();
        }

        private static void BuildThermostat()
        {
            Transform thermostat = new GameObject("Thermostat").transform;
            thermostat.SetParent(root);
            CreateCube("Thermostat body", new Vector3(14.6f, 2.1f, -6.7f), new Vector3(0.2f, 0.6f, 0.82f), Cream, false, 0.3f, 0.25f, 0f, thermostat);
            CreateCube("Thermostat screen", new Vector3(14.47f, 2.14f, -6.7f), new Vector3(0.025f, 0.3f, 0.5f), new Color(0.04f, 0.26f, 0.31f), false, 0.1f, 0.8f, 0.55f, thermostat);
            InteractionPoint point = thermostat.gameObject.AddComponent<InteractionPoint>();
            BoxCollider collider = thermostat.gameObject.AddComponent<BoxCollider>();
            collider.center = new Vector3(14.6f, 2.1f, -6.7f);
            collider.size = new Vector3(0.4f, 0.8f, 1f);
            point.Configure("把空调调到“省电模式”", () =>
            {
                game.World.ToggleLights();
                game.Session.AddSuspicion(5f);
                game.ReportNoise(new Vector3(14.2f, 0f, -6.7f), 7f, true);
                game.Toast("灯灭了。你突然拥有 0.8 秒的战术优势。");
            }, true, new Vector3(14.5f, 2.1f, -6.7f));
        }

        private static void BuildPoster()
        {
            Transform poster = new GameObject("Contest poster").transform;
            poster.SetParent(root);
            poster.position = new Vector3(14.52f, 2.75f, 5.9f);
            poster.rotation = Quaternion.Euler(0f, -90f, 0f);
            CreateCube("Poster frame", Vector3.zero, new Vector3(1.35f, 2.0f, 0.17f), Ink, false, 0.2f, 0.2f, 0f, poster);
            CreateCube("Poster paper", new Vector3(0f, 0f, -0.11f), new Vector3(1.12f, 1.75f, 0.025f), new Color(0.91f, 0.73f, 0.37f), false, 0.05f, 0.15f, 0f, poster);
            WorldLabel("KEEP\nTHINKING", poster, new Vector3(0f, 0.05f, -0.14f), 0.1f, new Color(0.15f, 0.14f, 0.12f), 55);

            InteractionPoint point = poster.gameObject.AddComponent<InteractionPoint>();
            BoxCollider collider = poster.gameObject.AddComponent<BoxCollider>();
            collider.center = Vector3.zero;
            collider.size = new Vector3(1.5f, 2.1f, 0.3f);
            point.Configure("掀起海报一角", () => game.RevealSideEnding(), true, new Vector3(0f, 0.05f, -0.22f));
        }

        private static void BuildLostManual()
        {
            Transform manual = new GameObject("Forgotten manual").transform;
            manual.SetParent(root);
            CreateCube("Manual book", new Vector3(14.25f, 0.08f, 5.85f), new Vector3(0.38f, 0.12f, 0.65f), new Color(0.76f, 0.38f, 0.13f), true, 0.2f, 0.2f, 0f, manual);
            manual.gameObject.SetActive(false);

            InteractionPoint point = manual.gameObject.AddComponent<InteractionPoint>();
            point.Configure("阅读手册最后一页", () => game.CompleteHiddenEnding(), true, new Vector3(14.25f, 0.08f, 5.85f));

            PosterWatcher watcher = root.gameObject.AddComponent<PosterWatcher>();
            watcher.Configure(game.Session, manual.gameObject);
        }

        private static void BuildInvigilators(ExamHallWorld world)
        {
            GameObject first = new GameObject("Invigilator A");
            first.transform.SetParent(root);
            first.transform.position = new Vector3(-12.3f, 0f, 5.2f);
            InvigilatorAgent firstAgent = first.AddComponent<InvigilatorAgent>();
            firstAgent.Configure(game, new[]
            {
                new Vector3(-12.3f, 0f, 5.2f),
                new Vector3(-12.3f, 0f, -7.6f),
                new Vector3(0f, 0f, -8.2f),
                new Vector3(12.3f, 0f, -7.6f),
                new Vector3(12.3f, 0f, 4.5f),
                new Vector3(6f, 0f, 5.0f),
                new Vector3(-5f, 0f, 5.0f)
            }, new Color(0.13f, 0.19f, 0.29f), "周老师");
            world.RegisterInvigilator(firstAgent);

            GameObject second = new GameObject("Invigilator B");
            second.transform.SetParent(root);
            second.transform.position = new Vector3(8.2f, 0f, 8.2f);
            InvigilatorAgent secondAgent = second.AddComponent<InvigilatorAgent>();
            secondAgent.Configure(game, new[]
            {
                new Vector3(8.2f, 0f, 8.2f),
                new Vector3(-8.1f, 0f, 8.2f),
                new Vector3(-10.6f, 0f, 4.3f),
                new Vector3(10.8f, 0f, 4.3f)
            }, new Color(0.24f, 0.17f, 0.19f), "陈老师");
            world.RegisterInvigilator(secondAgent);
        }

        private static void BuildDust()
        {
            GameObject dustObject = new GameObject("Dust motes");
            dustObject.transform.SetParent(root);
            dustObject.transform.position = new Vector3(0f, 2.5f, 0f);
            ParticleSystem particles = dustObject.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = particles.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(8f, 14f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.01f, 0.06f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.012f, 0.04f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.7f, 0.82f, 0.9f, 0.22f));
            main.maxParticles = 260;
            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = 24f;
            ParticleSystem.ShapeModule shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(25f, 4.8f, 18f);
            ParticleSystemRenderer renderer = dustObject.GetComponent<ParticleSystemRenderer>();
            Material particleMaterial = RenderAssets.CreateParticleMaterial(new Color(0.75f, 0.85f, 1f, 0.25f));
            if (particleMaterial != null)
            {
                renderer.material = particleMaterial;
            }
            particles.Play();
        }

        private static void BuildLabelOnWall(string text, Vector3 position, float size, Color color)
        {
            GameObject label = new GameObject("Wall label: " + text);
            label.transform.SetParent(root);
            TextMesh mesh = label.AddComponent<TextMesh>();
            mesh.text = text;
            mesh.font = FontLibrary.GetChineseFont();
            mesh.fontSize = 64;
            mesh.characterSize = size;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.color = color;
            label.transform.position = position;
        }

        private static GameObject WorldLabel(string text, Transform parent, Vector3 localPosition, float characterSize, Color color, int fontSize)
        {
            GameObject label = new GameObject("Label: " + text.Replace("\n", " "));
            label.transform.SetParent(parent);
            label.transform.localPosition = localPosition;
            TextMesh mesh = label.AddComponent<TextMesh>();
            mesh.text = text;
            mesh.font = FontLibrary.GetChineseFont();
            mesh.fontSize = fontSize;
            mesh.characterSize = characterSize;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.color = color;
            MeshRenderer renderer = label.GetComponent<MeshRenderer>();
            if (mesh.font != null && mesh.font.material != null)
            {
                renderer.sharedMaterial = mesh.font.material;
            }
            return label;
        }

        private static void WorldLabel(string text, Vector3 localPosition, float characterSize, Color color, int fontSize, Transform parent)
        {
            WorldLabel(text, parent, localPosition, characterSize, color, fontSize);
        }

        private static GameObject CreateCube(string objectName, Vector3 position, Vector3 scale, Color color, bool collider, float metallic, float gloss, float emission = 0f, Transform parent = null)
        {
            return CreatePrimitive(objectName, PrimitiveType.Cube, position, scale, color, collider, metallic, gloss, emission, parent);
        }

        private static GameObject CreateSphere(string objectName, Vector3 position, Vector3 scale, Color color, bool collider, float metallic, float gloss, float emission = 0f, Transform parent = null)
        {
            return CreatePrimitive(objectName, PrimitiveType.Sphere, position, scale, color, collider, metallic, gloss, emission, parent);
        }

        private static GameObject CreateCylinder(string objectName, Vector3 position, Vector3 scale, Color color, bool collider, float metallic, float gloss, float emission = 0f, Transform parent = null)
        {
            return CreatePrimitive(objectName, PrimitiveType.Cylinder, position, scale, color, collider, metallic, gloss, emission, parent);
        }

        private static GameObject CreateCapsule(string objectName, Vector3 position, Vector3 scale, Color color, Transform parent)
        {
            return CreatePrimitive(objectName, PrimitiveType.Capsule, position, scale, color, false, 0.05f, 0.18f, 0f, parent);
        }

        private static GameObject CreatePrimitive(
            string objectName,
            PrimitiveType type,
            Vector3 position,
            Vector3 scale,
            Color color,
            bool keepCollider,
            float metallic,
            float gloss,
            float emission,
            Transform parent)
        {
            GameObject instance = GameObject.CreatePrimitive(type);
            instance.name = objectName;
            instance.transform.SetParent(parent != null ? parent : root, false);
            instance.transform.localPosition = position;
            instance.transform.localScale = scale;

            if (!keepCollider)
            {
                Collider collider = instance.GetComponent<Collider>();
                if (collider != null)
                {
                    UnityEngine.Object.Destroy(collider);
                }
            }

            Renderer renderer = instance.GetComponent<Renderer>();
            Material material = Mat(color, metallic, gloss, emission);
            if (material != null)
            {
                renderer.sharedMaterial = material;
            }
            return instance;
        }

        private static Material Mat(Color color, float metallic = 0.05f, float gloss = 0.25f, float emission = 0f)
        {
            string key = ColorUtility.ToHtmlStringRGBA(color) + "_" + metallic.ToString("F2") + "_" + gloss.ToString("F2") + "_" + emission.ToString("F2");
            if (MaterialCache.TryGetValue(key, out Material existing))
            {
                return existing;
            }

            Material material = RenderAssets.CreateStandard(color, metallic, gloss, emission);
            if (material == null)
            {
                return null;
            }
            material.name = "NOI runtime material";
            MaterialCache[key] = material;
            return material;
        }

        private static void SetMaterial(GameObject target, Material material)
        {
            Renderer renderer = target.GetComponent<Renderer>();
            if (renderer != null && material != null)
            {
                renderer.sharedMaterial = material;
            }
        }

        private static System.Collections.IEnumerator DelayedToast(string message, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (game.Session != null && game.Session.Phase == GamePhase.Playing)
            {
                game.Toast(message);
            }
        }
    }

    public sealed class Rotator : MonoBehaviour
    {
        private Transform target;
        private float speed;

        public void Configure(Transform rotateTarget, float degreesPerSecond)
        {
            target = rotateTarget;
            speed = degreesPerSecond;
        }

        private void Update()
        {
            if (target != null)
            {
                target.Rotate(0f, speed * Time.deltaTime, 0f, Space.World);
            }
        }
    }

    public sealed class PosterWatcher : MonoBehaviour
    {
        private GameSession session;
        private GameObject manual;
        private bool shown;

        public void Configure(GameSession gameSession, GameObject manualObject)
        {
            session = gameSession;
            manual = manualObject;
        }

        private void Update()
        {
            if (!shown && session != null && session.SideRoomFound && manual != null)
            {
                shown = true;
                manual.SetActive(true);
            }
        }
    }
}
