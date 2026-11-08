using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AI_Smart_Teacher.Application.AI;
using AI_Smart_Teacher.Application.Curriculum;
using AI_Smart_Teacher.Application.Voice;
using AI_Smart_Teacher.Core.Enums;
using AI_Smart_Teacher.Desktop.Services;
using AI_Smart_Teacher.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;

namespace AI_Smart_Teacher.Desktop;

public partial class MainWindow : Window
{
    private readonly AuthViewModel _auth;
    private readonly CurriculumViewModel _curriculum;
    private readonly AiChatViewModel _ai;
    private readonly AssessmentViewModel _assessments;
    private readonly AdminViewModel _admin;

    public MainWindow()
    {
        InitializeComponent();
        _auth = App.Services.GetRequiredService<AuthViewModel>();
        _curriculum = App.Services.GetRequiredService<CurriculumViewModel>();
        _ai = App.Services.GetRequiredService<AiChatViewModel>();
        _assessments = App.Services.GetRequiredService<AssessmentViewModel>();
        _admin = App.Services.GetRequiredService<AdminViewModel>();
        LocalizationService.Apply(this, UiLanguage.Arabic);
        ContentTypeBox.ItemsSource = Enum.GetNames(typeof(ContentType));
        ContentTypeBox.SelectedItem = nameof(ContentType.LessonText);
        AiProviderBox.ItemsSource = new[] { "NotConfigured", "OfflineStudy", "OpenAICompatible" };
        AdminRoleBox.ItemsSource = Enum.GetValues<UserRoleType>().Select(r => r.ToString()).ToArray();
        UpdateLanguageText();
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        await _auth.RefreshModeAsync();
        if (_auth.CurrentUser is not null)
        {
            await _curriculum.LoadStagesAsync();
            await _ai.LoadSettingsAsync();
            await _assessments.RefreshAsync();
            await _admin.RefreshAsync();
        }
        RefreshUi();
    }

    private async void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        await _auth.LoginAsync(LoginUserBox.Text, LoginPasswordBox.Password);
        LoginPasswordBox.Clear();
        if (_auth.CurrentUser is not null)
        {
            await _curriculum.LoadStagesAsync();
            await _ai.LoadSettingsAsync();
            await _assessments.RefreshAsync();
            await _admin.RefreshAsync();
        }
        RefreshUi();
    }

    private async void RegisterButton_Click(object sender, RoutedEventArgs e)
    {
        var role = Enum.TryParse<UserRoleType>(RegisterRoleBox.SelectedItem?.ToString(), out var parsed) ? parsed : UserRoleType.Student;
        await _auth.RegisterAsync(RegisterUserBox.Text, RegisterEmailBox.Text, RegisterDisplayBox.Text, RegisterPasswordBox.Password, role);
        RegisterPasswordBox.Clear();
        await _auth.RefreshModeAsync();
        if (_auth.CurrentUser is not null)
        {
            await _curriculum.LoadStagesAsync();
            await _ai.LoadSettingsAsync();
            await _assessments.RefreshAsync();
        }
        RefreshUi();
    }

    private async void LogoutButton_Click(object sender, RoutedEventArgs e)
    {
        _auth.Logout();
        _ai.NewConversation();
        await _curriculum.LoadStagesAsync();
        RefreshUi();
    }

    private async void LanguageButton_Click(object sender, RoutedEventArgs e)
    {
        LocalizationService.Toggle(this);
        UpdateLanguageText();
        await _auth.RefreshModeAsync();
        RefreshUi();
    }

    private async void StageBox_SelectionChanged(object sender, SelectionChangedEventArgs e) { if (StageBox.SelectedItem is StageDto s) { await _curriculum.LoadGradesAsync(s.Id); RefreshCurriculum(); } }
    private async void GradeBox_SelectionChanged(object sender, SelectionChangedEventArgs e) { if (GradeBox.SelectedItem is GradeDto g) { await _curriculum.LoadCurriculaAsync(g.Id); RefreshCurriculum(); } }
    private async void CurriculumBox_SelectionChanged(object sender, SelectionChangedEventArgs e) { if (CurriculumBox.SelectedItem is CurriculumDto c) { await _curriculum.LoadSubjectsAsync(c.Id); RefreshCurriculum(); } }
    private async void SubjectBox_SelectionChanged(object sender, SelectionChangedEventArgs e) { if (SubjectBox.SelectedItem is SubjectDto s) { await _curriculum.LoadUnitsAsync(s.Id); RefreshCurriculum(); } }
    private async void UnitBox_SelectionChanged(object sender, SelectionChangedEventArgs e) { if (UnitBox.SelectedItem is UnitDto u) { await _curriculum.LoadLessonsAsync(u.Id); RefreshCurriculum(); } }
    private async void LessonBox_SelectionChanged(object sender, SelectionChangedEventArgs e) { if (LessonBox.SelectedItem is LessonDto l) { await _curriculum.LoadLessonAsync(l.Id); RefreshCurriculum(); } }

    private async Task RunManage(Func<Task<bool>> action, Func<Task>? reload = null)
    {
        await action();
        if (reload is not null) await reload();
        RefreshCurriculum();
        ShowCurriculumMessage();
    }

    private async void AddStageButton_Click(object sender, RoutedEventArgs e) => await RunManage(() => _curriculum.AddStageAsync(NewStageBox.Text, null), async () => await _curriculum.LoadStagesAsync());
    private async void AddGradeButton_Click(object sender, RoutedEventArgs e) { if (StageBox.SelectedItem is StageDto s) await RunManage(() => _curriculum.AddGradeAsync(s.Id, NewGradeBox.Text), () => _curriculum.LoadGradesAsync(s.Id)); else ShowPickParent(); }
    private async void AddCurriculumButton_Click(object sender, RoutedEventArgs e) { if (GradeBox.SelectedItem is GradeDto g) await RunManage(() => _curriculum.AddCurriculumAsync(g.Id, NewCurriculumBox.Text, null), () => _curriculum.LoadCurriculaAsync(g.Id)); else ShowPickParent(); }
    private async void AddSubjectButton_Click(object sender, RoutedEventArgs e) { if (CurriculumBox.SelectedItem is CurriculumDto c) await RunManage(() => _curriculum.AddSubjectAsync(c.Id, NewSubjectBox.Text, null), () => _curriculum.LoadSubjectsAsync(c.Id)); else ShowPickParent(); }
    private async void AddUnitButton_Click(object sender, RoutedEventArgs e) { if (SubjectBox.SelectedItem is SubjectDto s) await RunManage(() => _curriculum.AddUnitAsync(s.Id, NewUnitBox.Text), () => _curriculum.LoadUnitsAsync(s.Id)); else ShowPickParent(); }
    private async void AddLessonButton_Click(object sender, RoutedEventArgs e) { if (UnitBox.SelectedItem is UnitDto u) await RunManage(() => _curriculum.AddLessonAsync(u.Id, NewLessonBox.Text, null, null), () => _curriculum.LoadLessonsAsync(u.Id)); else ShowPickParent(); }
    private async void AddContentButton_Click(object sender, RoutedEventArgs e)
    {
        if (LessonBox.SelectedItem is not LessonDto l) { ShowPickParent(); return; }
        var type = Enum.TryParse<ContentType>(ContentTypeBox.SelectedItem?.ToString(), out var t) ? t : ContentType.LessonText;
        await RunManage(() => _curriculum.AddLessonContentAsync(l.Id, type, NewContentTitleBox.Text, NewContentBodyBox.Text, null, null), () => _curriculum.LoadLessonAsync(l.Id));
    }

    private void ShowPickParent() { _curriculum.GetType(); CurriculumMessageBorder.Visibility = Visibility.Visible; CurriculumMessageBorder.Background = new SolidColorBrush(Color.FromRgb(255, 240, 240)); CurriculumMessageText.Text = T("curriculum.pick"); }

    private void NewConversationButton_Click(object sender, RoutedEventArgs e) { _ai.NewConversation(); RefreshAi(); }
    private async void SendAiButton_Click(object sender, RoutedEventArgs e)
    {
        var text = AiInputBox.Text;
        AiInputBox.Clear();
        await _ai.SendAsync(text);
        RefreshAi();
    }

    private async void AttachImageButton_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Filter = "Images|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.webp|All files|*.*",
            CheckFileExists = true,
            Multiselect = false
        };
        if (dlg.ShowDialog(this) != true) return;
        AiStatusText.Text = T("ai.image.working");
        AiStatusText.Foreground = new SolidColorBrush(Color.FromRgb(20, 90, 78));
        await _ai.AttachImageAndAskAsync(dlg.FileName);
        RefreshAi();
    }

    private async void DictateButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_ai.CanUse) return;
        AiStatusText.Text = T("ai.voice.listening");
        AiStatusText.Foreground = new SolidColorBrush(Color.FromRgb(20, 90, 78));
        using var scope = App.Services.CreateScope();
        var stt = scope.ServiceProvider.GetRequiredService<ISpeechToTextService>();
        var lang = LocalizationService.Current == UiLanguage.Arabic ? "ar-SA" : "en-US";
        var result = await stt.RecognizeFromDefaultMicrophoneAsync(lang);
        if (result.Success && !string.IsNullOrWhiteSpace(result.Text))
        {
            AiInputBox.Text = string.IsNullOrWhiteSpace(AiInputBox.Text) ? result.Text : AiInputBox.Text + Environment.NewLine + result.Text;
            AiInputBox.CaretIndex = AiInputBox.Text.Length;
            AiStatusText.Text = T("ai.voice.ready");
        }
        else
        {
            AiStatusText.Text = result.Details ?? result.ErrorCode ?? T("ai.voice.failed");
            AiStatusText.Foreground = Brushes.Maroon;
        }
    }

    private async void SpeakButton_Click(object sender, RoutedEventArgs e)
    {
        var text = _ai.LastAssistantMessage;
        if (string.IsNullOrWhiteSpace(text)) { AiStatusText.Text = T("ai.voice.nothing"); return; }
        try
        {
            using var stream = await _ai.SynthesizeLastAssistantAsync();
            if (stream is null) { AiStatusText.Text = T("ai.voice.failed"); AiStatusText.Foreground = Brushes.Maroon; return; }
            var temp = Path.Combine(Path.GetTempPath(), $"ai-smart-teacher-tts-{Guid.NewGuid():N}.wav");
            await using (var fs = File.Create(temp))
                await stream.CopyToAsync(fs);
            var player = new System.Windows.Media.MediaPlayer();
            player.Open(new Uri(temp));
            player.MediaEnded += (_, _) => { try { player.Close(); File.Delete(temp); } catch { } };
            player.MediaFailed += (_, _) => { try { player.Close(); File.Delete(temp); } catch { } };
            player.Play();
            AiStatusText.Text = T("ai.voice.speaking");
            AiStatusText.Foreground = new SolidColorBrush(Color.FromRgb(20, 90, 78));
        }
        catch (Exception ex)
        {
            AiStatusText.Text = ex.GetType().Name;
            AiStatusText.Foreground = Brushes.Maroon;
        }
    }

    private async void SaveAiSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        var limit = int.TryParse(AiLimitBox.Text, out var n) ? n : 50;
        var dto = new AiSettingsDto(AiProviderBox.SelectedItem?.ToString() ?? "NotConfigured", AiModelBox.Text, AiEndpointBox.Text, false, AiCloudCheck.IsChecked == true, limit);
        await _ai.SaveSettingsAsync(dto, string.IsNullOrWhiteSpace(AiKeyBox.Password) ? null : AiKeyBox.Password);
        AiKeyBox.Clear();
        await _ai.LoadSettingsAsync();
        RefreshAi();
    }

    private void RefreshUi()
    {
        UpdateLanguageText();
        RegisterRoleBox.ItemsSource = _auth.AllowedRolesForRegistration();
        if (RegisterRoleBox.SelectedItem is null) RegisterRoleBox.SelectedIndex = 0;
        var user = _auth.CurrentUser;
        LogoutButton.Visibility = user is null ? Visibility.Collapsed : Visibility.Visible;
        CurriculumTab.IsEnabled = user is not null;
        AiTab.IsEnabled = user is not null;
        SessionBody.Text = user is null ? T("session.none") : $"{user.DisplayName} ({user.Username}) — {user.Role}\n" + T("session.scope");
        PermissionsList.ItemsSource = user is null ? Array.Empty<string>() : _curriculum.PermissionsFromSession();
        if (string.IsNullOrWhiteSpace(_auth.Message)) MessageBorder.Visibility = Visibility.Collapsed;
        else { MessageBorder.Visibility = Visibility.Visible; MessageBorder.Background = _auth.IsError ? new SolidColorBrush(Color.FromRgb(255,240,240)) : new SolidColorBrush(Color.FromRgb(236,252,247)); MessageText.Foreground = _auth.IsError ? Brushes.Maroon : new SolidColorBrush(Color.FromRgb(20,90,78)); MessageText.Text = _auth.Message; }
        RefreshCurriculum();
        ShowCurriculumMessage();
        RefreshAi();
        RefreshAssessmentsUi();
        RefreshAdminUi();
    }

    private void RefreshCurriculum()
    {
        var user = _auth.CurrentUser;
        var canView = user is not null;
        CurriculumTab.IsEnabled = canView;
        StageBox.ItemsSource = _curriculum.Stages; GradeBox.ItemsSource = _curriculum.Grades; CurriculumBox.ItemsSource = _curriculum.Curricula; SubjectBox.ItemsSource = _curriculum.Subjects; UnitBox.ItemsSource = _curriculum.Units; LessonBox.ItemsSource = _curriculum.Lessons;
        var detail = _curriculum.SelectedLesson;
        LessonDetailTitle.Text = detail?.Lesson.Title ?? T("curriculum.nolesson");
        LessonDetailBody.Text = detail is null ? T("curriculum.empty") : $"{detail.Lesson.Summary}\n{detail.Lesson.Objectives}".Trim();
        LessonContentsList.ItemsSource = detail?.Contents.Select(c => $"[{c.ContentType}] {c.Title}: {c.BodyText ?? c.MediaPath ?? c.SourceUrl ?? ""}") ?? Array.Empty<string>();
        var canManage = _curriculum.CanManage;
        foreach (var b in new[] { AddStageButton, AddGradeButton, AddCurriculumButton, AddSubjectButton, AddUnitButton, AddLessonButton, AddContentButton }) b.IsEnabled = canManage;
        ManageHint.Text = canManage ? T("manage.hint") : T("manage.readonly");
    }

    private void ShowCurriculumMessage()
    {
        if (string.IsNullOrWhiteSpace(_curriculum.Message)) { if (CurriculumMessageBorder.Visibility == Visibility.Visible && CurriculumMessageText.Text == T("curriculum.pick")) return; CurriculumMessageBorder.Visibility = Visibility.Collapsed; return; }
        CurriculumMessageBorder.Visibility = Visibility.Visible;
        CurriculumMessageBorder.Background = _curriculum.IsError ? new SolidColorBrush(Color.FromRgb(255,240,240)) : new SolidColorBrush(Color.FromRgb(236,252,247));
        CurriculumMessageText.Foreground = _curriculum.IsError ? Brushes.Maroon : new SolidColorBrush(Color.FromRgb(20,90,78));
        CurriculumMessageText.Text = _curriculum.Message;
    }

    private void RefreshAi()
    {
        var canUse = _ai.CanUse;
        var canManage = _ai.CanManageSettings;
        SendAiButton.IsEnabled = canUse; AiInputBox.IsEnabled = canUse; NewConversationButton.IsEnabled = canUse;
        AttachImageButton.IsEnabled = canUse; DictateButton.IsEnabled = canUse; SpeakButton.IsEnabled = canUse;
        AiProviderBox.IsEnabled = canManage; AiModelBox.IsEnabled = canManage; AiEndpointBox.IsEnabled = canManage; AiKeyBox.IsEnabled = canManage; AiCloudCheck.IsEnabled = canManage; AiLimitBox.IsEnabled = canManage; SaveAiSettingsButton.IsEnabled = canManage;
        AiMessagesList.ItemsSource = _ai.Messages.Select(m => $"{m.Role}: {m.Content}").ToList();
        if (string.IsNullOrWhiteSpace(AiStatusText.Text) || AiStatusText.Text == _ai.Status)
        {
            AiStatusText.Text = _ai.Status;
            AiStatusText.Foreground = _ai.IsError ? Brushes.Maroon : new SolidColorBrush(Color.FromRgb(20,90,78));
        }
        if (_ai.Settings is not null)
        {
            if (AiProviderBox.SelectedItem?.ToString() != _ai.Settings.ActiveProvider) AiProviderBox.SelectedItem = _ai.Settings.ActiveProvider;
            if (AiModelBox.Text != (_ai.Settings.Model ?? string.Empty)) AiModelBox.Text = _ai.Settings.Model ?? string.Empty;
            if (AiEndpointBox.Text != (_ai.Settings.Endpoint ?? string.Empty)) AiEndpointBox.Text = _ai.Settings.Endpoint ?? string.Empty;
            AiCloudCheck.IsChecked = _ai.Settings.AllowCloudRequests;
            if (AiLimitBox.Text != _ai.Settings.MaxRequestsPerDay.ToString()) AiLimitBox.Text = _ai.Settings.MaxRequestsPerDay.ToString();
        }
    }

    private void RefreshAssessmentsUi()
    {
        AssessmentTab.IsEnabled = _auth.CurrentUser is not null;
        var canManage = _assessments.CanManage;
        var canTake = _assessments.CanTake;
        AssessmentBox.ItemsSource = _assessments.Assessments;
        CreateAssessmentButton.IsEnabled = canManage; AddQuestionButton.IsEnabled = canManage; PublishAssessmentButton.IsEnabled = canManage; UnpublishAssessmentButton.IsEnabled = canManage;
        SubmitAssessmentButton.IsEnabled = canTake && _assessments.Selected?.Assessment.IsPublished == true;
        AssessmentAnswerHint.Text = canTake ? T("assessment.answer.hint") : T("assessment.answer.readonly");
        AssessmentDetailText.Text = _assessments.Selected is null ? T("assessment.none") : $"{_assessments.Selected.Assessment.Title} — {(_assessments.Selected.Assessment.IsPublished ? T("assessment.published") : T("assessment.draft"))} ({_assessments.Selected.Assessment.QuestionCount} {T("assessment.questions")})\n{_assessments.Selected.Assessment.Description}";
        RenderAssessmentQuestions(canManage, canTake && _assessments.Selected?.Assessment.IsPublished == true);
        ResultsText.Text = _assessments.Results.Count == 0 ? T("assessment.noresults") : string.Join("\n", _assessments.Results.Take(20).Select(r => $"{r.AssessmentTitle}: {r.Score}/{r.MaxScore} — {r.SubmittedAtUtc:u}"));
        ProgressText.Text = _assessments.Progress.Count == 0 ? T("assessment.noprogress") : string.Join("\n", _assessments.Progress.Take(20).Select(p => $"{p.CompletionPercent}% — lesson={p.LessonId} subject={p.SubjectId} {p.Strengths ?? p.Weaknesses ?? string.Empty}".Trim()));
        if (string.IsNullOrWhiteSpace(_assessments.Status)) AssessmentMessageBorder.Visibility = Visibility.Collapsed;
        else { AssessmentMessageBorder.Visibility = Visibility.Visible; AssessmentMessageBorder.Background = _assessments.IsError ? new SolidColorBrush(Color.FromRgb(255,240,240)) : new SolidColorBrush(Color.FromRgb(236,252,247)); AssessmentMessageText.Foreground = _assessments.IsError ? Brushes.Maroon : new SolidColorBrush(Color.FromRgb(20,90,78)); AssessmentMessageText.Text = _assessments.Status; }
    }

    private void RenderAssessmentQuestions(bool canManage, bool canTake)
    {
        AssessmentQuestionsPanel.Children.Clear();
        if (_assessments.Selected is null) return;
        foreach (var q in _assessments.Selected.Questions.OrderBy(q => q.SortOrder))
        {
            var border = new Border { Background = new SolidColorBrush(Color.FromRgb(248, 250, 252)), CornerRadius = new CornerRadius(8), Padding = new Thickness(10), Margin = new Thickness(0, 0, 0, 8), BorderBrush = new SolidColorBrush(Color.FromRgb(220, 228, 236)), BorderThickness = new Thickness(1) };
            var stack = new StackPanel();
            stack.Children.Add(new TextBlock { Text = q.Prompt, FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 6) });
            foreach (var o in q.Options.OrderBy(o => o.SortOrder))
            {
                var text = canManage && o.IsCorrect ? "✓ " + o.Text : o.Text;
                if (canTake)
                {
                    stack.Children.Add(new RadioButton { Content = text, GroupName = "q_" + q.Id.ToString("N"), Tag = o.Id, Margin = new Thickness(0, 0, 0, 4) });
                }
                else
                {
                    stack.Children.Add(new TextBlock { Text = "• " + text, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(8, 0, 0, 4), Foreground = canManage && o.IsCorrect ? new SolidColorBrush(Color.FromRgb(20, 90, 78)) : SystemColors.ControlTextBrush });
                }
            }
            border.Child = stack;
            AssessmentQuestionsPanel.Children.Add(border);
        }
    }

    private IEnumerable<Guid> GetSelectedAssessmentOptionIds()
        => AssessmentQuestionsPanel.Children.OfType<Border>()
            .Select(b => b.Child).OfType<StackPanel>()
            .SelectMany(s => s.Children.OfType<RadioButton>())
            .Where(r => r.IsChecked == true && r.Tag is Guid)
            .Select(r => (Guid)r.Tag!);

    private async void RefreshAssessmentsButton_Click(object sender, RoutedEventArgs e) { await _assessments.RefreshAsync(); RefreshAssessmentsUi(); }
    private async void AssessmentBox_SelectionChanged(object sender, SelectionChangedEventArgs e) { if (AssessmentBox.SelectedItem is AI_Smart_Teacher.Application.Assessments.AssessmentSummaryDto a) { await _assessments.LoadAssessmentAsync(a.Id); RefreshAssessmentsUi(); } }
    private async void CreateAssessmentButton_Click(object sender, RoutedEventArgs e) { await _assessments.CreateAsync(NewAssessmentBox.Text, NewAssessmentDescBox.Text, int.TryParse(AssessmentDurationBox.Text, out var d) ? d : 20); RefreshAssessmentsUi(); }
    private async void AddQuestionButton_Click(object sender, RoutedEventArgs e) { if (_assessments.Selected is null) return; await _assessments.AddMultipleChoiceAsync(_assessments.Selected.Assessment.Id, NewQuestionBox.Text, decimal.TryParse(QuestionPointsBox.Text, out var p) ? p : 1, QuestionOptionsEntryBox.Text); RefreshAssessmentsUi(); }
    private async void PublishAssessmentButton_Click(object sender, RoutedEventArgs e) { if (_assessments.Selected is null) return; await _assessments.PublishAsync(_assessments.Selected.Assessment.Id, true); RefreshAssessmentsUi(); }
    private async void UnpublishAssessmentButton_Click(object sender, RoutedEventArgs e) { if (_assessments.Selected is null) return; await _assessments.PublishAsync(_assessments.Selected.Assessment.Id, false); RefreshAssessmentsUi(); }
    private async void SubmitAssessmentButton_Click(object sender, RoutedEventArgs e) { await _assessments.SubmitCurrentAsync(GetSelectedAssessmentOptionIds()); RefreshAssessmentsUi(); }

    private void RefreshAdminUi()
    {
        var visible = _admin.CanViewUsers || _admin.CanViewReports || _admin.CanManageSettings;
        AdminTab.IsEnabled = visible && _auth.CurrentUser is not null;
        RefreshAdminButton.IsEnabled = AdminTab.IsEnabled;
        ActivateUserButton.IsEnabled = _admin.CanManageUsers; DeactivateUserButton.IsEnabled = _admin.CanManageUsers; SetRoleButton.IsEnabled = _admin.CanManageUsers; AdminRoleBox.IsEnabled = _admin.CanManageUsers;
        BackupDbButton.IsEnabled = _admin.CanManageSettings; SaveSettingButton.IsEnabled = _admin.CanManageSettings; SettingKeyBox.IsEnabled = _admin.CanManageSettings; SettingValueBox.IsEnabled = _admin.CanManageSettings; SettingCategoryBox.IsEnabled = _admin.CanManageSettings;
        AdminUserBox.ItemsSource = _admin.Users;
        if (_admin.SystemStatus is not null) AdminStatusText.Text = $"Users={_admin.SystemStatus.Users} Assessments={_admin.SystemStatus.Assessments} Conversations={_admin.SystemStatus.Conversations} Attachments={_admin.SystemStatus.Attachments}\nDB={_admin.SystemStatus.DatabasePath}\nBackups={_admin.SystemStatus.BackupsDirectory}";
        AdminAuditText.Text = _admin.AuditLogs.Count == 0 ? T("admin.noaudit") : string.Join(Environment.NewLine, _admin.AuditLogs.Take(80).Select(a => $"{a.CreatedAtUtc:u} [{a.Action}] {a.EntityName}:{a.EntityId} {a.Details}"));
        AdminSettingsText.Text = _admin.Settings.Count == 0 ? T("admin.nosettings") : string.Join(Environment.NewLine, _admin.Settings.Select(s => $"{s.Key} = {s.Value} ({s.Category}){(s.IsSensitive ? " [sensitive]" : "")}"));
        if (string.IsNullOrWhiteSpace(_admin.Status)) AdminMessageBorder.Visibility = Visibility.Collapsed;
        else { AdminMessageBorder.Visibility = Visibility.Visible; AdminMessageBorder.Background = _admin.IsError ? new SolidColorBrush(Color.FromRgb(255,240,240)) : new SolidColorBrush(Color.FromRgb(236,252,247)); AdminMessageText.Foreground = _admin.IsError ? Brushes.Maroon : new SolidColorBrush(Color.FromRgb(20,90,78)); AdminMessageText.Text = _admin.Status; }
    }

    private async void RefreshAdminButton_Click(object sender, RoutedEventArgs e) { await _admin.RefreshAsync(); RefreshAdminUi(); }
    private async void ActivateUserButton_Click(object sender, RoutedEventArgs e) { if (AdminUserBox.SelectedItem is AI_Smart_Teacher.Application.Admin.AdminUserDto u) { await _admin.SetActiveAsync(u.Id, true); RefreshAdminUi(); } }
    private async void DeactivateUserButton_Click(object sender, RoutedEventArgs e) { if (AdminUserBox.SelectedItem is AI_Smart_Teacher.Application.Admin.AdminUserDto u) { await _admin.SetActiveAsync(u.Id, false); RefreshAdminUi(); } }
    private async void SetRoleButton_Click(object sender, RoutedEventArgs e) { if (AdminUserBox.SelectedItem is AI_Smart_Teacher.Application.Admin.AdminUserDto u && Enum.TryParse<UserRoleType>(AdminRoleBox.SelectedItem?.ToString(), out var role)) { await _admin.SetRoleAsync(u.Id, role); RefreshAdminUi(); } }
    private async void BackupDbButton_Click(object sender, RoutedEventArgs e) { await _admin.BackupAsync(); RefreshAdminUi(); }
    private async void SaveSettingButton_Click(object sender, RoutedEventArgs e) { await _admin.SaveSettingAsync(SettingKeyBox.Text, SettingValueBox.Text, SettingCategoryBox.Text); RefreshAdminUi(); }

    private void UpdateLanguageText()
    {
        AuthTab.Header = T("tab.auth"); CurriculumTab.Header = T("tab.curriculum"); AiTab.Header = T("tab.ai"); AssessmentTab.Header = T("tab.assessments"); AdminTab.Header = T("tab.admin");
        AuthTitle.Text = _auth.BootstrapMode ? T("auth.bootstrap.title") : T("auth.title");
        AuthHint.Text = _auth.BootstrapMode ? T("auth.bootstrap.hint") : T("auth.hint");
        LoginHeader.Text = T("login.header"); LoginUserLabel.Text = T("login.user"); LoginPasswordLabel.Text = T("login.password"); LoginButton.Content = T("login.button");
        RegisterHeader.Text = T("register.header"); RegisterUserLabel.Text = T("register.user"); RegisterEmailLabel.Text = T("register.email"); RegisterDisplayLabel.Text = T("register.display"); RegisterRoleLabel.Text = T("register.role"); RegisterPasswordLabel.Text = T("register.password"); RegisterButton.Content = _auth.BootstrapMode ? T("register.bootstrap.button") : T("register.button");
        LogoutButton.Content = T("logout.button"); SessionTitle.Text = T("session.title"); PermissionsTitle.Text = T("permissions.title"); PhaseTitle.Text = T("phase.title"); PhaseBody.Text = T("phase.body");
        CurriculumTitle.Text = T("curriculum.title"); CurriculumHint.Text = T("curriculum.hint"); StageLabel.Text=T("stage"); GradeLabel.Text=T("grade"); CurriculumLabel.Text=T("curriculum"); SubjectLabel.Text=T("subject"); UnitLabel.Text=T("unit"); LessonLabel.Text=T("lesson");
        ManageTitle.Text=T("manage.title"); NewStageLabel.Text=T("new.stage"); NewGradeLabel.Text=T("new.grade"); NewCurriculumLabel.Text=T("new.curriculum"); NewSubjectLabel.Text=T("new.subject"); NewUnitLabel.Text=T("new.unit"); NewLessonLabel.Text=T("new.lesson"); NewContentLabel.Text=T("new.content");
        AddStageButton.Content=T("add"); AddGradeButton.Content=T("add"); AddCurriculumButton.Content=T("add"); AddSubjectButton.Content=T("add"); AddUnitButton.Content=T("add"); AddLessonButton.Content=T("add"); AddContentButton.Content=T("add.content");
        AdminTitle.Text=T("admin.title"); AdminHint.Text=T("admin.hint"); RefreshAdminButton.Content=T("admin.refresh"); AdminStatusLabel.Text=T("admin.status"); AdminUsersLabel.Text=T("admin.users"); ActivateUserButton.Content=T("admin.activate"); DeactivateUserButton.Content=T("admin.deactivate"); AdminRoleLabel.Text=T("admin.role"); SetRoleButton.Content=T("admin.setrole"); AdminAuditLabel.Text=T("admin.audit"); AdminSystemTitle.Text=T("admin.system"); AdminSystemHint.Text=T("admin.system.hint"); BackupDbButton.Content=T("admin.backup"); SettingKeyLabel.Text=T("admin.setting.key"); SettingValueLabel.Text=T("admin.setting.value"); SettingCategoryLabel.Text=T("admin.setting.category"); SaveSettingButton.Content=T("admin.setting.save"); AdminSettingsLabel.Text=T("admin.settings");
        AiTitle.Text=T("ai.title"); AiHint.Text=T("ai.hint"); NewConversationButton.Content=T("ai.new"); AttachImageButton.Content=T("ai.image"); DictateButton.Content=T("ai.dictate"); SpeakButton.Content=T("ai.speak"); SendAiButton.Content=T("ai.send"); AiSettingsTitle.Text=T("ai.settings"); AiSettingsHint.Text=T("ai.settings.hint");
        AiProviderLabel.Text=T("ai.provider"); AiModelLabel.Text=T("ai.model"); AiEndpointLabel.Text=T("ai.endpoint"); AiKeyLabel.Text=T("ai.key"); AiCloudCheck.Content=T("ai.cloud"); AiLimitLabel.Text=T("ai.limit"); SaveAiSettingsButton.Content=T("ai.save"); AiSafetyText.Text=T("ai.safety");
        AssessmentTitle.Text=T("assessment.title"); AssessmentHint.Text=T("assessment.hint"); RefreshAssessmentsButton.Content=T("assessment.refresh"); AssessmentsLabel.Text=T("assessment.list"); SubmitAssessmentButton.Content=T("assessment.submit"); ResultsLabel.Text=T("assessment.results"); ProgressLabel.Text=T("assessment.progress");
        ManageAssessmentTitle.Text=T("assessment.manage"); ManageAssessmentHint.Text=T("assessment.manage.hint"); NewAssessmentLabel.Text=T("assessment.new"); NewAssessmentDescLabel.Text=T("assessment.desc"); DurationLabel.Text=T("assessment.duration"); CreateAssessmentButton.Content=T("assessment.create"); NewQuestionLabel.Text=T("assessment.question"); QuestionPointsLabel.Text=T("assessment.points"); QuestionOptionsEntryLabel.Text=T("assessment.options"); AddQuestionButton.Content=T("assessment.addq"); PublishAssessmentButton.Content=T("assessment.publish"); UnpublishAssessmentButton.Content=T("assessment.unpublish");
    }

    private static string T(string key)
    {
        var ar = LocalizationService.Current == UiLanguage.Arabic;
        return key switch
        {
            "tab.auth" => ar ? "الحساب" : "Account",
            "tab.curriculum" => ar ? "المناهج والدروس" : "Curricula & Lessons",
            "tab.ai" => ar ? "المعلم الذكي" : "AI Tutor",
            "tab.assessments" => ar ? "الاختبارات والتقدم" : "Assessments & Progress",
            "tab.admin" => ar ? "الإدارة" : "Admin",
            "admin.title" => ar ? "أساس لوحة الإدارة" : "Admin foundation", "admin.hint" => ar ? "عرض/إدارة أساسية للمستخدمين وسجل التدقيق والإعدادات غير الحساسة والنسخ الاحتياطي. يتطلب صلاحيات إدارية مناسبة (users.manage / users.view / reports.view.all / system.settings.manage)." : "Basic user management, audit log, non-sensitive settings, and backup foundation. Requires appropriate admin permissions (users.manage / users.view / reports.view.all / system.settings.manage).", "admin.refresh" => ar ? "تحديث الإدارة" : "Refresh admin", "admin.status" => ar ? "حالة النظام" : "System status", "admin.users" => ar ? "المستخدمون" : "Users", "admin.activate" => ar ? "تفعيل" : "Activate", "admin.deactivate" => ar ? "تعطيل" : "Deactivate", "admin.role" => ar ? "تغيير الدور" : "Set role", "admin.setrole" => ar ? "حفظ الدور" : "Save role", "admin.audit" => ar ? "سجل التدقيق" : "Audit log", "admin.noaudit" => ar ? "لا توجد سجلات تدقيق بعد." : "No audit logs yet.", "admin.system" => ar ? "النظام والإعدادات" : "System & settings", "admin.system.hint" => ar ? "النسخ الاحتياطي يستخدم خدمة SQLite المحلية. الإعدادات الحساسة (مثل مفاتيح AI المحمية) لا تُعرض كنص ولا يمكن تعديلها من هذا النموذج البسيط." : "Backup uses the local SQLite backup service. Sensitive settings (such as protected AI keys) are not shown as plain text and cannot be edited from this simple form.", "admin.backup" => ar ? "إنشاء نسخة احتياطية" : "Create backup", "admin.setting.key" => ar ? "مفتاح الإعداد" : "Setting key", "admin.setting.value" => ar ? "القيمة" : "Value", "admin.setting.category" => ar ? "التصنيف (اختياري)" : "Category (optional)", "admin.setting.save" => ar ? "حفظ إعداد غير حساس" : "Save non-sensitive setting", "admin.settings" => ar ? "الإعدادات المرئية" : "Visible settings", "admin.nosettings" => ar ? "لا توجد إعدادات مرئية." : "No visible settings.",
            "auth.title" => ar ? "تسجيل الدخول / إنشاء حساب" : "Sign in / Create account",
            "auth.bootstrap.title" => ar ? "التهيئة الأولى للنظام" : "Initial system setup",
            "auth.hint" => ar ? "سجّل الدخول بحساب موجود. إنشاء حساب مسؤول من هذه الشاشة غير متاح بعد التهيئة الأولى." : "Sign in with an existing account. Admin creation from this screen is disabled after initial setup.",
            "auth.bootstrap.hint" => ar ? "لا يوجد مستخدمون بعد. أنشئ أول حساب (يفضل مسؤول Admin) لبدء استخدام النظام. كلمات المرور تُجزّأ محليًا ولا تُخزَّن كنص واضح." : "No users exist yet. Create the first account (preferably Admin) to start using the system. Passwords are hashed locally and never stored in plain text.",
            "login.header" => ar ? "تسجيل الدخول" : "Sign in", "login.user" => ar ? "اسم المستخدم أو البريد" : "Username or email", "login.password" => ar ? "كلمة المرور" : "Password", "login.button" => ar ? "دخول" : "Sign in",
            "register.header" => ar ? "إنشاء حساب" : "Create account", "register.user" => ar ? "اسم المستخدم" : "Username", "register.email" => ar ? "البريد الإلكتروني (اختياري)" : "Email (optional)", "register.display" => ar ? "الاسم الظاهر" : "Display name", "register.role" => ar ? "نوع الحساب" : "Account type", "register.password" => ar ? "كلمة المرور (8 أحرف على الأقل)" : "Password (at least 8 characters)", "register.button" => ar ? "إنشاء حساب" : "Create account", "register.bootstrap.button" => ar ? "إنشاء أول حساب" : "Create first account",
            "logout.button" => ar ? "تسجيل الخروج" : "Sign out", "session.title" => ar ? "الجلسة الحالية" : "Current session", "session.none" => ar ? "لا يوجد مستخدم مسجل الدخول." : "No user is signed in.", "session.scope" => ar ? "المراحل المنفذة: حسابات وصلاحيات، مناهج ودروس، معلم ذكي مع حفظ محادثات وإعدادات مزود قابلة للتبديل، صور/OCR وصوت Windows أساسي عند توفرهما، اختبارات/نتائج وتقدم محلي أساسي، وأساس إدارة محلي (مستخدمون/سجل تدقيق/إعدادات غير حساسة/نسخ احتياطي). ما يزال خارج النطاق: تقارير متقدمة، ربط ولي أمر كامل، استعادة قاعدة بيانات من الواجهة، دفع/اشتراكات، وتحليلات." : "Implemented: accounts/permissions, curricula/lessons, AI tutor with saved conversations and pluggable provider settings, Windows OCR/speech where available, basic local assessments/results/progress, and an admin foundation (users/audit/non-sensitive settings/backup). Still out of scope: advanced reports, full parent linking UI, UI restore, billing/subscriptions, and analytics.",
            "permissions.title" => ar ? "الصلاحيات المفعّلة للدور" : "Enabled role permissions", "phase.title" => ar ? "حالة التنفيذ" : "Implementation status", "phase.body" => ar ? "AI سحابي اختياري ويحتاج إنترنت + مفتاح API صالح؛ لا مفاتيح في الكود والمفتاح المحفوظ محمي محليًا بـ DPAPI (CurrentUser). OCR والصوت يعتمدان على موارد/أذونات Windows وقد يفشلان بأمان. تصحيح الأسئلة المقالية/المفتوحة يتطلب مراجعة يدوية. أساس الإدارة متاح حسب الصلاحيات ولا يعرض القيم الحساسة كنص." : "Cloud AI is optional and requires internet + a valid API key; no keys in code and saved keys are protected locally with current-user DPAPI. OCR/speech depend on Windows resources/permissions and may fail safely. Essay/short-answer grading requires manual review. The admin foundation is permission-gated and does not expose sensitive values as plain text.",
            "ai.title" => ar ? "محادثة المعلم الذكي" : "AI Smart Teacher chat", "ai.hint" => ar ? "اسأل عن درس أو مسألة، أو أرفق صورة لمسألة/نص (OCR محلي عبر Windows). المحادثات تُحفظ محليًا. المزود السحابي اختياري." : "Ask about a lesson or problem, or attach an image/problem text (local Windows OCR). Conversations are saved locally. Cloud AI is optional.",
            "ai.new" => ar ? "محادثة جديدة" : "New conversation", "ai.send" => ar ? "إرسال" : "Send", "ai.settings" => ar ? "إعدادات AI" : "AI settings", "ai.settings.hint" => ar ? "إدارة مزود AI تتطلب ai.settings.manage. الصور/الصوت/المحادثة تتطلب ai.use." : "AI provider management requires ai.settings.manage. Image/voice/chat require ai.use.",
            "ai.provider" => ar ? "المزود" : "Provider", "ai.model" => ar ? "النموذج" : "Model", "ai.endpoint" => ar ? "عنوان API (OpenAI-compatible)" : "API endpoint (OpenAI-compatible)", "ai.key" => ar ? "مفتاح API جديد (اختياري عند الحفظ)" : "New API key (optional on save)", "ai.cloud" => ar ? "السماح بطلبات AI السحابية" : "Allow cloud AI requests", "ai.limit" => ar ? "الحد اليومي لرسائل المستخدم" : "Daily user message limit", "ai.save" => ar ? "حفظ إعدادات AI" : "Save AI settings",
            "ai.safety" => ar ? "تنبيه: لا تضع مفاتيح API في الكود. المفاتيح المحفوظة تُحمى محليًا بـ DPAPI للمستخدم الحالي. OCR والصوت خدمات Windows محلية وقد تتطلب لغة/صلاحية." : "Notice: do not put API keys in code. Saved keys are protected locally with current-user DPAPI. OCR/speech use local Windows services and may require language/permission.",
            "ai.image" => ar ? "إرفاق صورة/OCR" : "Attach image/OCR", "ai.dictate" => ar ? "إملاء صوتي" : "Dictate", "ai.speak" => ar ? "قراءة الرد" : "Speak reply", "ai.image.working" => ar ? "جارٍ تحليل الصورة وOCR..." : "Analyzing image and OCR...", "ai.voice.listening" => ar ? "جارٍ الاستماع (خدمة Windows)..." : "Listening (Windows speech)...", "ai.voice.ready" => ar ? "تم إدراج النص المُملَى." : "Dictated text inserted.", "ai.voice.failed" => ar ? "تعذر تنفيذ عملية الصوت. تحقق من أذونات الميكروفون/اللغة." : "Speech operation failed. Check microphone permissions/language.", "ai.voice.nothing" => ar ? "لا يوجد رد مساعد لقراءته." : "No assistant reply to speak.", "ai.voice.speaking" => ar ? "جارٍ قراءة آخر رد..." : "Speaking last reply...",
            "assessment.title" => ar ? "الاختبارات والتقدم" : "Assessments & progress", "assessment.hint" => ar ? "اختبارات محلية أساسية: إنشاء/نشر (مدرس/مسؤول)، حل اختيارات متعددة/صح-خطأ آليًا (طالب)، وعرض النتائج والتقدم. الأسئلة المقالية تُحفظ للمراجعة اليدوية." : "Basic local assessments: create/publish (teacher/admin), auto-score multiple-choice/true-false (student), and view results/progress. Essay questions are saved for manual review.", "assessment.refresh" => ar ? "تحديث" : "Refresh", "assessment.list" => ar ? "الاختبارات" : "Assessments", "assessment.none" => ar ? "اختر اختبارًا." : "Select an assessment.", "assessment.published" => ar ? "منشور" : "Published", "assessment.draft" => ar ? "مسودة" : "Draft", "assessment.questions" => ar ? "سؤال" : "questions", "assessment.answer.hint" => ar ? "اختر إجابة واحدة لكل سؤال موضوعي ثم اضغط إرسال الإجابات. التصحيح الآلي للاختيارات فقط." : "Choose one answer per objective question, then submit. Auto-scoring applies to selected options only.", "assessment.answer.readonly" => ar ? "عرض الأسئلة فقط. يلزم حساب بصلاحية assessments.take وحساب طالب/جلسة مناسبة للحل." : "Question view only. Solving requires assessments.take and an appropriate signed-in account.", "assessment.submit" => ar ? "إرسال الإجابات" : "Submit answers", "assessment.results" => ar ? "النتائج" : "Results", "assessment.progress" => ar ? "التقدم" : "Progress", "assessment.noresults" => ar ? "لا توجد نتائج بعد." : "No results yet.", "assessment.noprogress" => ar ? "لا يوجد سجل تقدم بعد." : "No progress records yet.", "assessment.manage" => ar ? "إدارة الاختبارات" : "Manage assessments", "assessment.manage.hint" => ar ? "يتطلب assessments.manage. لإنشاء سؤال اختيارات متعددة: اكتب كل خيار في سطر، وضع * قبل الخيار الصحيح." : "Requires assessments.manage. For multiple choice, put each option on a new line and prefix the correct option with *.", "assessment.new" => ar ? "عنوان اختبار جديد" : "New assessment title", "assessment.desc" => ar ? "الوصف (اختياري)" : "Description (optional)", "assessment.duration" => ar ? "المدة بالدقائق" : "Duration (minutes)", "assessment.create" => ar ? "إنشاء اختبار" : "Create assessment", "assessment.question" => ar ? "نص السؤال" : "Question prompt", "assessment.points" => ar ? "الدرجة" : "Points", "assessment.options" => ar ? "الخيارات (سطر لكل خيار، * للصحيح)" : "Options (one per line, * = correct)", "assessment.addq" => ar ? "إضافة سؤال اختيارات" : "Add multiple-choice question", "assessment.publish" => ar ? "نشر" : "Publish", "assessment.unpublish" => ar ? "إلغاء النشر" : "Unpublish",
            "curriculum.title" => ar ? "تصفح المناهج والدروس" : "Browse curricula and lessons", "curriculum.hint" => ar ? "اختر التسلسل من المرحلة حتى الدرس. المحتوى المعروض محلي من قاعدة البيانات." : "Select from stage down to lesson. Displayed content comes from the local database.",
            "stage" => ar ? "المرحلة" : "Stage", "grade" => ar ? "الصف" : "Grade", "curriculum" => ar ? "المنهج" : "Curriculum", "subject" => ar ? "المادة" : "Subject", "unit" => ar ? "الوحدة" : "Unit", "lesson" => ar ? "الدرس" : "Lesson",
            "curriculum.nolesson" => ar ? "لم يتم اختيار درس" : "No lesson selected", "curriculum.empty" => ar ? "اختر درسًا لعرض الملخص والمحتوى." : "Select a lesson to view summary and content.", "curriculum.pick" => ar ? "اختر العنصر الأب المناسب قبل الإضافة." : "Select the appropriate parent item before adding.",
            "manage.title" => ar ? "إدارة المحتوى التعليمي" : "Manage learning content", "manage.hint" => ar ? "متاح للحسابات التي تملك curriculum.manage (مثل Admin/Teacher)." : "Available to accounts with curriculum.manage permission (e.g., Admin/Teacher).", "manage.readonly" => ar ? "حسابك الحالي يمكنه التصفح فقط وفق الصلاحيات." : "Your current account has read/browse access according to permissions.",
            "new.stage" => ar ? "مرحلة جديدة" : "New stage", "new.grade" => ar ? "صف جديد (يتطلب مرحلة)" : "New grade (requires stage)", "new.curriculum" => ar ? "منهج جديد (يتطلب صفًا)" : "New curriculum (requires grade)", "new.subject" => ar ? "مادة جديدة (يتطلب منهجًا)" : "New subject (requires curriculum)", "new.unit" => ar ? "وحدة جديدة (يتطلب مادة)" : "New unit (requires subject)", "new.lesson" => ar ? "درس جديد (يتطلب وحدة)" : "New lesson (requires unit)", "new.content" => ar ? "محتوى درس (يتطلب درسًا): العنوان/النوع/النص" : "Lesson content (requires lesson): title/type/text", "add" => ar ? "إضافة" : "Add", "add.content" => ar ? "إضافة محتوى" : "Add content",
            _ => key
        };
    }
}