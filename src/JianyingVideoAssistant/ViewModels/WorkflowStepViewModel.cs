using System.Windows.Input;
using JianyingVideoAssistant.Infrastructure;

namespace JianyingVideoAssistant.ViewModels;

public sealed class WorkflowStepViewModel : ObservableObject
{
    private string _summary;

    public WorkflowStepViewModel(
        string indexLabel,
        string glyph,
        string title,
        string description,
        string summary,
        string actionLabel,
        string availabilityLabel,
        bool isActionEnabled,
        ICommand? primaryCommand)
    {
        IndexLabel = indexLabel;
        Glyph = glyph;
        Title = title;
        Description = description;
        _summary = summary;
        ActionLabel = actionLabel;
        AvailabilityLabel = availabilityLabel;
        IsActionEnabled = isActionEnabled;
        PrimaryCommand = primaryCommand;
    }

    public string IndexLabel { get; }
    public string Glyph { get; }
    public string Title { get; }
    public string Description { get; }
    public string Summary
    {
        get => _summary;
        set => SetProperty(ref _summary, value);
    }
    public string ActionLabel { get; }
    public string AvailabilityLabel { get; }
    public bool IsActionEnabled { get; }
    public ICommand? PrimaryCommand { get; }
}
