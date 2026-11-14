#nullable enable

using Godot;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Timer = Godot.Timer;

namespace Kaguya
{
    public class MoonTextData
    {
        public string text { get; set; } = string.Empty;
        public float? time { get; set; }
    }

    public partial class KaguyaMoonTextPool : Node2D
    {
        private const string ScriptPath = "res://MoonText/MoonText.gd";
        private readonly List<Control> _pool = new();
        private Script? _script;
        private bool _reportedFailure;

        public Control GetInstance()
        {
            _pool.RemoveAll(node => !IsInstanceValid(node) || node.IsQueuedForDeletion());
            foreach (var node in _pool)
            {
                if (!node.Visible)
                {
                    node.Visible = true;
                    return node;
                }
            }
            // The game's room cache disposes evicted resources, even if C# still holds them.
            // Own a lazily loaded script for this room instead of borrowing that cache's wrapper.
            if (!IsInstanceValid(_script))
                _script = ResourceLoader.Load<Script>(ScriptPath, cacheMode: ResourceLoader.CacheMode.Ignore);
            if (!IsInstanceValid(_script) || !_script!.CanInstantiate())
                throw new InvalidOperationException($"Cannot instantiate moon text script: {ScriptPath}");

            var newNode = new Control { Name = $"MoonTextContainer_{_pool.Count}" };
            try
            {
                newNode.SetScript(_script);
                if (!newNode.HasMethod("spawn"))
                    throw new InvalidOperationException($"Moon text script has no spawn method: {ScriptPath}");
                AddChild(newNode);
                _pool.Add(newNode);
                return newNode;
            }
            catch
            {
                newNode.Free();
                throw;
            }
        }

        internal void ReportFailure(Exception exception)
        {
            _script = null;
            if (_reportedFailure) return;
            _reportedFailure = true;
            GD.PushWarning($"[Kaguya] Skipping moon text effect; combat will continue. {ScriptPath}: {exception}");
        }

        public override void _ExitTree()
        {
            _pool.Clear();
            _script = null;
        }
    }

    public static class KaguyaMoonText
    {
        private static KaguyaMoonTextPool? GetPool()
        {
            var combatRoom = NCombatRoom.Instance;
            if (!GodotObject.IsInstanceValid(combatRoom) || !combatRoom!.IsInsideTree() || combatRoom.IsQueuedForDeletion())
                return null;

            // Rooms overlap briefly during transitions. Never keep the previous room's pool.
            var pool = combatRoom.GetNodeOrNull<KaguyaMoonTextPool>("KaguyaMoonTextPool");
            if (!GodotObject.IsInstanceValid(pool) || pool!.IsQueuedForDeletion())
            {
                pool = new KaguyaMoonTextPool { Name = "KaguyaMoonTextPool" };
                combatRoom.AddChild(pool);
            }
            return pool;
        }

        public static void Spawn(string text, Vector2 position)
        {
            SpawnCore(text, position);
        }

        public static void Spawn(string text, Vector2 position,
            float typingDuration, float floatDelay, float floatTime)
        {
            var plainText = Regex.Replace(text, @"\[(\/?[^\]]+)\]", "");
            float charDelay = plainText.Length > 0 ? typingDuration / plainText.Length : 0.05f;
            SpawnCore(text, position, node =>
            {
                node.Set("char_delay", charDelay);
                node.Set("float_delay", floatDelay);
                node.Set("float_time", floatTime);
            });
        }

        public static void SpawnInstant(string text, Vector2 position,
            float floatDelay = 3f, float floatTime = 2f, float jitterStrength = 2f)
        {
            SpawnCore(text, position, node =>
            {
                node.Set("char_delay", 0f);
                node.Set("float_delay", floatDelay);
                node.Set("float_time", floatTime);
                node.Set("jitter_strength", jitterStrength);
            });
        }

        private static void SpawnCore(string text, Vector2 position, Action<Control>? configure = null)
        {
            KaguyaMoonTextPool? pool = null;
            Control? node = null;
            try
            {
                pool = GetPool();
                if (pool == null) return;
                node = pool.GetInstance();
                node.Set("text", text);
                configure?.Invoke(node);
                node.Position = position;
                node.Call("spawn");
            }
            catch (Exception exception)
            {
                // Decorative text must not interrupt AfterAddedToRoom and prevent the first turn.
                if (GodotObject.IsInstanceValid(node)) node!.QueueFree();
                if (pool != null) pool.ReportFailure(exception);
                else GD.PushWarning($"[Kaguya] Skipping moon text effect; combat will continue. {exception}");
            }
        }

        public static void SpawnRandom(string text)
        {
            float x = (float)GD.RandRange(150.0, 1300.0);
            float y = (float)GD.RandRange(200.0, 650.0);
            Spawn(text, new Vector2(x, y));
        }

        public static void StartAuto(NCombatRoom combatRoom, MoonTextData[] data)
        {
            if (data == null || data.Length == 0) return;
            bool allHaveTime = data.All(d => d.time.HasValue);
            bool noneHaveTime = data.All(d => !d.time.HasValue);
            if (allHaveTime)
                StartSequential(combatRoom, data);
            else if (noneHaveTime)
                StartRandom(combatRoom, data);
        }

        public static void StartSequential(NCombatRoom combatRoom, MoonTextData[] data)
        {
            if (data == null || data.Length == 0) return;
            int index = 0;
            var timer = new Timer();
            timer.OneShot = true;
            combatRoom.AddChild(timer);

            void ScheduleNext()
            {
                if (index >= data.Length) return;
                float currentTime = data[index].time!.Value;
                int batchEnd = index;
                while (batchEnd < data.Length && data[batchEnd].time == currentTime)
                    batchEnd++;
                float waitTime;
                if (batchEnd < data.Length)
                    waitTime = data[batchEnd].time!.Value - currentTime;
                else
                    waitTime = 5f;
                if (waitTime > 10f) waitTime = 5f;
                float typingDuration = waitTime / 5f;
                float floatDelay = waitTime * 4f / 5f;
                float floatTime = 2f;
                for (int i = index; i < batchEnd; i++)
                {
                    float x = (float)GD.RandRange(0.0, 1350.0);
                    float y = (float)GD.RandRange(200.0, 650.0);
                    Spawn(data[i].text, new Vector2(x, y),
                        typingDuration, floatDelay, floatTime);
                }
                index = batchEnd;
                if (index < data.Length)
                {
                    timer.WaitTime = data[index].time!.Value - currentTime;
                    timer.Start();
                }
            }
            timer.WaitTime = data[0].time!.Value;
            timer.Timeout += ScheduleNext;
            timer.Start();
        }

        public static void StartRandom(NCombatRoom combatRoom, MoonTextData[] data)
        {
            if (data == null || data.Length == 0) return;
            var timer = new Timer();
            timer.WaitTime = 5f;
            timer.OneShot = false;
            timer.Autostart = true;
            combatRoom.AddChild(timer);
            timer.Timeout += () =>
            {
                float x = (float)GD.RandRange(150.0, 1300.0);
                float y = (float)GD.RandRange(200.0, 650.0);
                var item = data[(int)(GD.Randi() % (uint)data.Length)];
                Spawn(item.text, new Vector2(x, y));
            };
        }
    }
}
