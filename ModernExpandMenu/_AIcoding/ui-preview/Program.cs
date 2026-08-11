using System;
using System.Drawing;
using System.Windows.Forms;
using ModernSettingsUI;

namespace CyberPreview
{
    /// <summary>
    /// 赛博开关独立 demo（不依赖 RimWorld，纯 WinForms）：
    /// 完全使用 Gemini 移植的 CyberSwitchCard.cs，模拟 demo1「Ultimate Cyberpunk Switch」的效果。
    /// 深色背景 + 标题 + 两个卡片开关（一个激活、一个未激活），点击卡片切换并触发扫光/冲击波动效。
    /// </summary>
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var form = new Form
            {
                Text = "Cyber Switch Demo - 赛博开关预览",
                BackColor = Color.FromArgb(10, 12, 16),      // --md-surface #0a0c10
                ClientSize = new Size(484, 268),
                FormBorderStyle = FormBorderStyle.FixedSingle,
                MaximizeBox = false,
                StartPosition = FormStartPosition.CenterScreen,
                Font = new Font("Segoe UI", 10f)
            };

            // 标题（对齐 demo .title：主色 + 发光感）
            var title = new Label
            {
                Text = "背景全动态赛博开关 (Ultra Style B)",
                ForeColor = Color.FromArgb(0, 168, 255),     // --md-primary #00a8ff
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(22, 16)
            };
            form.Controls.Add(title);

            // 开关 1：激活态（对齐 demo cyber1）
            var switch1 = new CyberSwitchCard
            {
                Location = new Point(22, 58),
                Title = "超高帧率与渲染加速",
                Description = "开启 120Hz 动态渲染与全局 GPU 补帧",
                Checked = true
            };
            form.Controls.Add(switch1);

            // 开关 2：未激活态（对齐 demo cyber2）
            var switch2 = new CyberSwitchCard
            {
                Location = new Point(22, 58 + 76 + 18),
                Title = "空间音效与立体声增强",
                Description = "开启右键菜单弹出 Sound 3D 视听反馈"
            };
            form.Controls.Add(switch2);

            Application.Run(form);
        }
    }
}
