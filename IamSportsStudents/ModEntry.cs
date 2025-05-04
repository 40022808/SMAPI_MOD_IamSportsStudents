using System;
using Microsoft.Xna.Framework;
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
            helper.Events.Display.RenderedHud += this.OnRenderedHud;
        }

        private void OnRenderedHud(object sender, RenderedHudEventArgs e)
        {
            // **如果正在过剧情，直接退出，不绘制文本**
            if (Game1.eventUp || Game1.activeClickableMenu != null)
            {
                return;
            }

            // 获取玩家当前体力和体力上限
            int currentStamina = (int)Game1.player.stamina;
            int maxStamina = (int)Game1.player.maxStamina.Value;

            // 如果体力值超过 10000，则转换为 "万" 单位，保留 1 位小数
            string formattedStamina = currentStamina >= 10000 ? $"{currentStamina / 10000f:F0}万" : $"{currentStamina}";
            string formattedMaxStamina = maxStamina >= 10000 ? $"{maxStamina / 10000f:F0}万" : $"{maxStamina}";

            // 获取当前 UI 状态（是否显示生命条）
            bool isHealthBarVisible = Game1.showingHealthBar;

            // **如果生命条可见，则额外左移一定距离**
            int offset = isHealthBarVisible ? 50 : 0;

            // 定义位置
            int barX = (int)(Game1.viewport.Width * 0.96) - offset;
            int barY = Game1.viewport.Height - 55;

            // 数值文本
            string staminaText = $"体力:{formattedStamina}/{formattedMaxStamina}";

            // 计算文本宽度
            float textWidth = Game1.dialogueFont.MeasureString(staminaText).X;

            // 调整文本位置，使其左移一定距离
            Vector2 textPosition = new Vector2(barX - textWidth, barY);

            // 设定文本描边的偏移值
            Vector2[] outlineOffsets = new Vector2[]
            {
                new Vector2(-2, 0),  // 左
                new Vector2(2, 0),   // 右
                new Vector2(0, -2),  // 上
                new Vector2(0, 2)    // 下
            };

            // 绘制黑色描边（先绘制黑色文本）
            foreach (Vector2 offsetVector in outlineOffsets)
            {
                e.SpriteBatch.DrawString(Game1.dialogueFont, staminaText, textPosition + offsetVector, Color.Black);
            }

            // 绘制原本的白色文本（覆盖黑色描边）
            e.SpriteBatch.DrawString(Game1.dialogueFont, staminaText, textPosition, Color.White);
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
            initialphysicalstrengthlimit = Game1.player.maxStamina.Value - staminaData.ExtraStamina;

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
