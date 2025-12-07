using System.Windows.Markup;

// XAML에서 사용할 네임스페이스 정의
// 사용 예: <Window xmlns:jino="https://github.com/JinoPay/Jinobald">

// Application & Hosting
[assembly: XmlnsDefinition("https://github.com/JinoPay/Jinobald", "Jinobald.Wpf.Application")]
[assembly: XmlnsDefinition("https://github.com/JinoPay/Jinobald", "Jinobald.Wpf.Hosting")]

// MVVM
[assembly: XmlnsDefinition("https://github.com/JinoPay/Jinobald", "Jinobald.Wpf.Mvvm")]

// Services
[assembly: XmlnsDefinition("https://github.com/JinoPay/Jinobald", "Jinobald.Wpf.Services.Regions")]
[assembly: XmlnsDefinition("https://github.com/JinoPay/Jinobald", "Jinobald.Wpf.Services.Dialog")]
[assembly: XmlnsDefinition("https://github.com/JinoPay/Jinobald", "Jinobald.Wpf.Services.Navigation")]
[assembly: XmlnsDefinition("https://github.com/JinoPay/Jinobald", "Jinobald.Wpf.Services.Theme")]
[assembly: XmlnsDefinition("https://github.com/JinoPay/Jinobald", "Jinobald.Wpf.Services.Events")]

// xmlns 접두사를 "jino"로 권장
[assembly: XmlnsPrefix("https://github.com/JinoPay/Jinobald", "jino")]
