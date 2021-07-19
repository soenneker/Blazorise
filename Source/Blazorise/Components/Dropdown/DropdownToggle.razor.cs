﻿#region Using directives
using System;
using System.Threading.Tasks;
using Blazorise.States;
using Blazorise.Utilities;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
#endregion

namespace Blazorise
{
    /// <summary>
    /// Toggles the dropdown menu visibility on or off.
    /// </summary>
    public partial class DropdownToggle : BaseComponent, ICloseActivator
    {
        #region Members

        private bool split;

        private bool disabled;

        private bool jsRegistered;

        private DotNetObjectReference<CloseActivatorAdapter> dotNetObjectRef;

        private DropdownState parentDropdownState;

        #endregion

        #region Methods

        /// <inheritdoc/>
        protected override void OnInitialized()
        {
            if ( Theme != null )
            {
                Theme.Changed += OnThemeChanged;
            }

            base.OnInitialized();
        }

        /// <inheritdoc/>
        protected override Task OnFirstAfterRenderAsync()
        {
            dotNetObjectRef ??= CreateDotNetObjectRef( new CloseActivatorAdapter( this ) );

            return base.OnFirstAfterRenderAsync();
        }

        /// <inheritdoc/>
        protected override void BuildClasses( ClassBuilder builder )
        {
            builder.Append( ClassProvider.DropdownToggle() );
            builder.Append( ClassProvider.DropdownToggleColor( Color ), Color != Color.None && !Outline );
            builder.Append( ClassProvider.DropdownToggleOutline( Color ), Color != Color.None && Outline );
            builder.Append( ClassProvider.DropdownToggleSize( ThemeSize ), ThemeSize != Blazorise.Size.None );
            builder.Append( ClassProvider.DropdownToggleSplit(), Split );
            builder.Append( ClassProvider.DropdownToggleIcon( IsToggleIconVisible ) );

            base.BuildClasses( builder );
        }

        /// <summary>
        /// Disposes all the used resources.
        /// </summary>
        /// <param name="disposing">True if object is disposing.</param>
        protected override async ValueTask DisposeAsync( bool disposing )
        {
            if ( disposing && Rendered )
            {
                // make sure to unregister listener
                if ( jsRegistered )
                {
                    jsRegistered = false;

                    var task = JSRunner.UnregisterClosableComponent( this );

                    try
                    {
                        await task;
                    }
                    catch when ( task.IsCanceled )
                    {
                    }
                }

                DisposeDotNetObjectRef( dotNetObjectRef );
                dotNetObjectRef = null;

                if ( Theme != null )
                {
                    Theme.Changed -= OnThemeChanged;
                }
            }

            await base.DisposeAsync( disposing );
        }

        /// <summary>
        /// Handles the item onclick event.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        protected Task ClickHandler()
        {
            if ( !Disabled )
            {
                ParentDropdown?.Toggle();
            }

            return Clicked.InvokeAsync( null );
        }

        /// <summary>
        /// Returns true of the parent dropdown-menu is safe to be closed.
        /// </summary>
        /// <param name="elementId">Id of an element.</param>
        /// <param name="closeReason">Close reason.</param>
        /// <param name="isChildClicked">Indicates if the child element was clicked.</param>
        /// <returns>True if it's safe to be closed.</returns>
        public Task<bool> IsSafeToClose( string elementId, CloseReason closeReason, bool isChildClicked )
        {
            return Task.FromResult( closeReason == CloseReason.EscapeClosing || elementId != ElementId );
        }

        /// <summary>
        /// Forces the parent dropdown to close the dropdown-menu.
        /// </summary>
        /// <param name="closeReason">Reason for closing the parent.</param>
        /// <returns>Returns the awaitable task.</returns>
        public Task Close( CloseReason closeReason )
        {
            ParentDropdown?.Hide();

            return Task.CompletedTask;
        }

        /// <summary>
        /// Sets focus on the input element, if it can be focused.
        /// </summary>
        /// <param name="scrollToElement">If true the browser should scroll the document to bring the newly-focused element into view.</param>
        public void Focus( bool scrollToElement = true )
        {
            _ = JSRunner.Focus( ElementRef, ElementId, scrollToElement );
        }

        /// <summary>
        /// Handles the visibility styles and JS interop states.
        /// </summary>
        /// <param name="visible">True if component is visible.</param>
        protected virtual void HandleVisibilityStyles( bool visible )
        {
            if ( visible )
            {
                jsRegistered = true;

                ExecuteAfterRender( async () =>
                {
                    await JSRunner.RegisterClosableComponent( dotNetObjectRef, ElementRef );
                } );
            }
            else
            {
                jsRegistered = false;

                ExecuteAfterRender( async () =>
                {
                    await JSRunner.UnregisterClosableComponent( this );
                } );
            }

            DirtyClasses();
            DirtyStyles();
        }

        /// <summary>
        /// An event raised when theme settings changes.
        /// </summary>
        /// <param name="sender">An object that raised the event.</param>
        /// <param name="eventArgs"></param>
        private void OnThemeChanged( object sender, EventArgs eventArgs )
        {
            DirtyClasses();
            DirtyStyles();

            InvokeAsync( StateHasChanged );
        }

        #endregion

        #region Properties

        /// <inheritdoc/>
        protected override bool ShouldAutoGenerateId => true;

        /// <summary>
        /// True if parent dropdown is part of a button group.
        /// </summary>
        protected bool IsGroup => ParentDropdown?.IsGroup == true;

        /// <summary>
        /// True if the toggle button should be disabled.
        /// </summary>
        protected bool IsDisabled => ParentDropdown?.Disabled ?? Disabled;

        /// <summary>
        /// Should the toggle icon be drawn
        /// </summary>
        protected bool IsToggleIconVisible => ToggleIconVisible.GetValueOrDefault( Theme?.DropdownOptions?.ToggleIconVisible ?? true );

        /// <summary>
        /// Gets the size based on the theme settings.
        /// </summary>
        protected Size ThemeSize => Size ?? Theme?.DropdownOptions?.Size ?? Blazorise.Size.None;

        /// <summary>
        /// Gets the data-boundary value.
        /// </summary>
        protected string DataBoundary
            => ParentDropdown?.InResponsiveTable == true ? "window" : null;

        /// <summary>
        /// Gets or sets the dropdown color.
        /// </summary>
        [Parameter] public Color Color { get; set; } = Color.None;

        /// <summary>
        /// Gets or sets the dropdown size.
        /// </summary>
        [Parameter] public Size? Size { get; set; }

        /// <summary>
        /// Button outline.
        /// </summary>
        [Parameter] public bool Outline { get; set; }

        /// <summary>
        /// Indicates that a toggle should act as a split button.
        /// </summary>
        [Parameter]
        public bool Split
        {
            get => split;
            set
            {
                split = value;

                DirtyClasses();
            }
        }

        /// <summary>
        /// Makes the toggle element look inactive.
        /// </summary>
        [Parameter]
        public bool Disabled
        {
            get => disabled;
            set
            {
                disabled = value;

                DirtyClasses();
            }
        }

        /// <summary>
        /// Gets or sets the parent dropdown state object.
        /// </summary>
        [CascadingParameter]
        protected DropdownState ParentDropdownState
        {
            get => parentDropdownState;
            set
            {
                if ( parentDropdownState == value )
                    return;

                parentDropdownState = value;

                HandleVisibilityStyles( parentDropdownState.Visible );
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the dropdown toggle icon is visible.
        /// </summary>
        /// <value>
        /// <c>true</c> if [show toggle]; otherwise, <c>false</c>.
        /// </value>
        /// <remarks>Default: True</remarks>
        [Parameter] public bool? ToggleIconVisible { get; set; }

        /// <summary>
        /// If defined, indicates that its element can be focused and can participates in sequential keyboard navigation.
        /// </summary>
        [Parameter] public int? TabIndex { get; set; }

        /// <summary>
        /// Occurs when the toggle button is clicked.
        /// </summary>
        [Parameter] public EventCallback Clicked { get; set; }

        /// <summary>
        /// The applied theme.
        /// </summary>
        [CascadingParameter] protected Theme Theme { get; set; }

        /// <summary>
        /// Gets or sets the reference to the parent dropdown.
        /// </summary>
        [CascadingParameter] protected Dropdown ParentDropdown { get; set; }

        /// <summary>
        /// Specifies the content to be rendered inside this <see cref="DropdownToggle"/>.
        /// </summary>
        [Parameter] public RenderFragment ChildContent { get; set; }

        #endregion
    }
}