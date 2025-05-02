using System;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace IamSportsStudents
{
    public class ModEntry : Mod
    {
        private float previousStamina;
        private float accumulatedStaminaLoss; // 新增：累计体力消耗
        private float initialphysicalstrengthlimit;
        private const string StaminaKey = "IamSportsStudents_ExtraStamina"; // 存储体力增量的键名

        public override void Entry(IModHelper helper)
        {
            helper.Events.GameLoop.DayStarted += this.OnDayStarted; // 每天开始时应用额外体力
            helper.Events.GameLoop.UpdateTicked += this.OnUpdateTicked; // 监听体力消耗
        }

        /// <summary>在每日开始时应用额外体力，确保提升的数据不会丢失。</summary>
        private void OnDayStarted(object sender, DayStartedEventArgs e)
        {
            if (!Context.IsWorldReady)
                return;

            StaminaData staminaData = this.Helper.Data.ReadGlobalData<StaminaData>(StaminaKey) ?? new StaminaData();

            // **存储原始体力上限**
            if (initialphysicalstrengthlimit == 0)
            {
                initialphysicalstrengthlimit = Game1.player.maxStamina.Value; // 只记录一次游戏初始的体力上限
            }

            // **防止无限增长：只有当最大体力增长大于当前模组累计值时，才更新**
            int expectedMaxStamina = (int)(initialphysicalstrengthlimit + staminaData.ExtraStamina);
            if (Game1.player.maxStamina.Value > expectedMaxStamina)
            {
                initialphysicalstrengthlimit = Game1.player.maxStamina.Value - staminaData.ExtraStamina;
                this.Monitor.Log($"检测到玩家体力提升（可能是星之果实），更新原始体力上限为 {initialphysicalstrengthlimit}", LogLevel.Info);
            }

            int ExtraMaxStamina = (int)(staminaData.ExtraStamina + initialphysicalstrengthlimit);

            LoadPermanentExtraStamina(ExtraMaxStamina);
            Game1.player.stamina = Game1.player.maxStamina.Value;
        }

        /// <summary>检测体力消耗，并累积消耗数据来提升最大体力。</summary>
        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (!Context.IsWorldReady)
                return;

            float currentStamina = Game1.player.Stamina;
            float staminaLost = previousStamina - currentStamina;

            if (staminaLost > 0) // 只累积消耗的体力（不会因体力恢复影响计算）
            {
                accumulatedStaminaLoss += staminaLost; // 累积体力消耗
                this.Monitor.Log($"玩家累计消耗了 {accumulatedStaminaLoss} 体力", LogLevel.Info);
            }

            if (accumulatedStaminaLoss >= 10) // 当累计消耗达到 10 点时，提升最大体力
            {
                int extraStamina = this.Helper.Data.ReadGlobalData<StaminaData>(StaminaKey)?.ExtraStamina ?? 0;
                int extraStamina2;

                extraStamina2 = 1;
                extraStamina = extraStamina + extraStamina2; // 每消耗 10 体力，增加 1 最大体力
                
                accumulatedStaminaLoss = accumulatedStaminaLoss - 10; // 重置消耗计数，仅保留未满 10 的体力消耗
                this.Helper.Data.WriteGlobalData(StaminaKey, new StaminaData { ExtraStamina = extraStamina });

                ApplyPermanentStaminaIncrease(extraStamina2);
                this.Monitor.Log($"玩家永久提升体力至 {Game1.player.maxStamina.Value + extraStamina2}, 提升量:[{extraStamina2}],之前体力上限[{Game1.player.maxStamina.Value}]", LogLevel.Info);
            }

            previousStamina = currentStamina;
        }

        /// <summary>应用永久的体力提升，不依赖 Buff。</summary>
        private void ApplyPermanentStaminaIncrease(int extraStamina)
        {
            Game1.player.maxStamina.Value = Game1.player.maxStamina.Value + extraStamina;
        }

        private void LoadPermanentExtraStamina(int ExtraMaxStamina)
        {
            Game1.player.maxStamina.Value = ExtraMaxStamina;
        }

    }

    public class StaminaData
    {
        public int ExtraStamina { get; set; } = 0;

    }
}
