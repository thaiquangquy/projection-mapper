using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using ReactiveUI;
using Splat;
using TeamSketch.DependencyInjection;
using TeamSketch.Services;

namespace TeamSketch.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    public ReactiveCommand<Unit, Unit> UndoCommand { get; }
    public ReactiveCommand<Unit, Unit> RedoCommand { get; }
    public ReactiveCommand<Unit, Unit> NewCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenCommand { get; }
    public ReactiveCommand<Unit, Unit> SaveCommand { get; }
    public ReactiveCommand<Unit, Unit> SaveAsCommand { get; }
    public ReactiveCommand<Unit, Unit> QuitCommand { get; }

    public event Action RequestUndo;
    public event Action RequestRedo;
    public event Action RequestNewFile;
    public event Action RequestOpenFile;
    public event Action RequestClose;
    public event Action<bool> RequestSave;
    private string ImageFileName;

    public MainWindowViewModel()
    {
        UndoCommand = ReactiveCommand.Create(Undo);
        RedoCommand = ReactiveCommand.Create(Redo);
        NewCommand = ReactiveCommand.Create(New);
        OpenCommand = ReactiveCommand.Create(Open);
        SaveCommand = ReactiveCommand.Create(Save);
        SaveAsCommand = ReactiveCommand.Create(SaveAs);
        QuitCommand = ReactiveCommand.Create(Quit);
    }

    public void Undo()
    {
        RequestUndo();
    }
    public void Redo()
    {
        RequestRedo();
    }
    public void New()
    {
        RequestNewFile();
    }
    public void Open()
    {
        RequestOpenFile();
    }
    public async void Save()
    {
        RequestSave(false);
    }
    public void SaveAs()
    {
        RequestSave(true);
    }
    public void Quit()
    {
        RequestClose();
    }

    private bool undoEnabled = false;
    public bool UndoEnabled
    {
        get => undoEnabled;
        set => this.RaiseAndSetIfChanged(ref undoEnabled, value);
    }

    private bool redoEnabled;
    public bool RedoEnabled
    {
        get => redoEnabled;
        set => this.RaiseAndSetIfChanged(ref redoEnabled, value);
    }

    private bool saveEnabled;
    public bool SaveEnabled
    {
        get => saveEnabled;
        set => this.RaiseAndSetIfChanged(ref saveEnabled, value);
    }

    private bool saveAsEnabled;
    public bool SaveAsEnabled
    {
        get => saveAsEnabled;
        set => this.RaiseAndSetIfChanged(ref saveAsEnabled, value);
    }
}
