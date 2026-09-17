Namespace Services.Theming

    ''' <summary>
    ''' Service that manages the application theme, allowing live swapping of the color palette 
    ''' and persistence of the user's choice.
    ''' </summary>
    ''' <remarks>
    ''' The live-swap mechanism works by replacing the active palette ResourceDictionary in
    ''' Application.Current.Resources.MergedDictionaries at runtime.
    ''' For controls to dynamically update their styling without restarting, all color and brush 
    ''' references in XAML must use DynamicResource (e.g. Background="{DynamicResource WindowBackgroundBrush}").
    ''' </remarks>
    Public Interface IThemeService

        ''' <summary>
        ''' Gets the currently active application theme.
        ''' </summary>
        ReadOnly Property Current As AppTheme

        ''' <summary>
        ''' Applies the specified theme by hot-swapping the palette dictionary.
        ''' </summary>
        ''' <param name="theme">The theme to apply.</param>
        Sub Apply(theme As AppTheme)

        ''' <summary>
        ''' Toggles the theme between Light and Dark.
        ''' </summary>
        Sub Toggle()

        ''' <summary>
        ''' Loads the persisted theme choice from disk. Fallback to Light if not found or on error.
        ''' </summary>
        ''' <returns>The persisted AppTheme.</returns>
        Function LoadPersisted() As AppTheme

    End Interface

End Namespace
