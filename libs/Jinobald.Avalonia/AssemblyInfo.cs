using Avalonia.Metadata;

// XAML에서 사용할 네임스페이스 정의
// 사용 예: <Window xmlns:jino="https://github.com/JinoPay/Jinobald">

// Application & Hosting
[assembly: XmlnsDefinition("https://github.com/JinoPay/Jinobald", "Jinobald.Avalonia.Application")]
[assembly: XmlnsDefinition("https://github.com/JinoPay/Jinobald", "Jinobald.Avalonia.Hosting")]

// MVVM
[assembly: XmlnsDefinition("https://github.com/JinoPay/Jinobald", "Jinobald.Avalonia.Mvvm")]

// Controls
[assembly: XmlnsDefinition("https://github.com/JinoPay/Jinobald", "Jinobald.Avalonia.Controls")]

// Services
[assembly: XmlnsDefinition("https://github.com/JinoPay/Jinobald", "Jinobald.Avalonia.Services.Regions")]
[assembly: XmlnsDefinition("https://github.com/JinoPay/Jinobald", "Jinobald.Avalonia.Services.Dialog")]
[assembly: XmlnsDefinition("https://github.com/JinoPay/Jinobald", "Jinobald.Avalonia.Services.Theme")]
[assembly: XmlnsDefinition("https://github.com/JinoPay/Jinobald", "Jinobald.Avalonia.Services.Events")]

// xmlns 접두사를 "jino"로 권장
[assembly: XmlnsPrefix("https://github.com/JinoPay/Jinobald", "jino")]
