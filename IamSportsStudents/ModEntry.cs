using System;
using GenericModConfigMenu;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;

namespace IamSportsStudents
{
    public class ModEntry : Mod
    {
        private float previousStamina;
        private float accumulatedStaminaLoss; // 新增：累计体力消耗
        private float initialphysicalstrengthlimit;
        private const string StaminaKey = "IamSportsStudents_ExtraStamina"; // 存储体力增量的键名
        private ModConfig? Config; //获取配置
        


        public override void Entry(IModHelper helper)
        {
            this.Config = this.Helper.ReadConfig<ModConfig>();
            int Increase = this.Config.Increase;
            int AddingStandards = this.Config.AddingStandards;
            helper.Events.GameLoop.DayStarted += this.OnDayStarted; // 每天开始时应用额外体力
            helper.Events.GameLoop.UpdateTicked += this.OnUpdateTicked; // 监听体力消耗       
            helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
            

        }


        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            // get Generic Mod Config Menu's API (if it's installed)
            var configMenu = this.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu is null)
                return;

            // register mod
            configMenu.Register(
                mod: this.ModManifest,
                reset: () => this.Config = new ModConfig(),
                save: () => this.Helper.WriteConfig(this.Config)
            );

            // add some config options
            configMenu.AddSectionTitle(
                mod:this.ModManifest,
                text: () => "我是体育生"
                );

            configMenu.AddNumberOption(
                mod: this.ModManifest,
                name: () => "增加量",
                tooltip: () => "每次增加多少点体力上限，范围 1-100",
                getValue: () => this.Config.Increase,
                setValue: value => this.Config.Increase = value,
                min: 1,
                max: 100
            );
            configMenu.AddNumberOption(
                mod: this.ModManifest,
                name: () => "增加标准",
                tooltip: () => "影响消耗多少点体力增长一次体力上限，范围 1-200",
                getValue: () => this.Config.AddingStandards,
                setValue: value => this.Config.AddingStandards = value,
                min: 1,
                max: 200
            );
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
            
            int Increase = this.Config.Increase;
            int AddingStandards = this.Config.AddingStandards;

            
            if (staminaLost > 0) // 只累积消耗的体力（不会因体力恢复影响计算）
            {
                accumulatedStaminaLoss += staminaLost; // 累积体力消耗
                

            }

            if (accumulatedStaminaLoss >= AddingStandards) // 当累计消耗达到 10 点时，提升最大体力
            {
                
                int extraStamina = this.Helper.Data.ReadGlobalData<StaminaData>(StaminaKey)?.ExtraStamina ?? 0;
                int extraStamina2;

                extraStamina2 = Increase;
                extraStamina = extraStamina + extraStamina2; // 每消耗 10 体力，增加 1 最大体力
                
                accumulatedStaminaLoss = accumulatedStaminaLoss - AddingStandards; // 重置消耗计数，仅保留未满 10 的体力消耗
                this.Helper.Data.WriteGlobalData(StaminaKey, new StaminaData { ExtraStamina = extraStamina });

                ApplyPermanentStaminaIncrease(extraStamina2);
                
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
