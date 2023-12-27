﻿using Avalonia.Controls;
using Meadow.Workbench.ViewModels;
using System;

namespace Meadow.Workbench.Services;

public interface IFeature
{
    public string Title { get; }
    public Type ViewType { get; }
    public Type ViewModelType { get; }
    public Lazy<UserControl> ViewInstance { get; }
    public Lazy<FeatureViewModel> ViewModelInstance { get; }
}
