import { Code2, MessageCircle, UserRound } from "lucide-react";

export function About() {
  return <section className="human-page about-page">
    <div className="human-card about-intro">
      <div><h2>剪映视频助手</h2><span>独立运行的 Windows 视频生产客户端</span></div>
      <p>用于完成素材归类、内容文库管理、背景音乐管理和剪映草稿生成。</p>
    </div>
    <div className="about-credit-grid">
      <article className="human-card about-credit-card"><Code2 /><div><small>开发工具</small><b>Codex（OpenAI）</b><span>参与需求分析、界面与程序实现、测试和发布维护。</span></div></article>
      <article className="human-card about-credit-card"><UserRound /><div><small>项目协助</small><b>Gryps</b><span>参与产品方向、业务规则、测试验收和持续改进。</span></div></article>
      <article className="human-card about-credit-card"><MessageCircle /><div><small>微信</small><b>gryps_zhang</b><span>项目协助者的联系微信。</span></div></article>
    </div>
  </section>;
}
