using Content.Client.Lobby.UI;
using Content.Client.Message;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.LinkAccount;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using static Robust.Client.UserInterface.Controls.BaseButton;
using static Robust.Client.UserInterface.Controls.LineEdit;
using static Robust.Client.UserInterface.Controls.TabContainer;

namespace Content.Client._RMC14.LinkAccount;

public sealed class LinkAccountUIController : UIController, IOnSystemChanged<LinkAccountSystem>
{
    [Dependency] private readonly IClipboardManager _clipboard = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IUriOpener _uriOpener = default!;

    private LinkAccountWindow? _window;
    private PatronPerksWindow? _patronPerksWindow;
    private TimeSpan _disableUntil;

    private Guid _code;

    private void OnCode(Guid code)
    {
        _code = code;

        if (_window == null)
            return;

        _window.CopyButton.Disabled = false;
    }

    private void OnUpdated()
    {
        if (UIManager.ActiveScreen is not LobbyGui gui)
            return;
    }

    private void OnLobbyMessageReceived(SharedRMCDisplayLobbyMessageEvent message)
    {
        if (UIManager.ActiveScreen is not LobbyGui gui)
            return;

        var user = FormattedMessage.EscapeText(message.User);
        var msg = FormattedMessage.EscapeText(message.Message);
    }

    public void ToggleWindow()
    {
        if (_window == null)
        {
            _window = new LinkAccountWindow();
            _window.OnClose += () => _window = null;
            _window.Label.SetMarkupPermissive($"{Loc.GetString("rmc-ui-link-discord-account-text")}");

            {
                _clipboard.SetText(_code.ToString());
                _window.CopyButton.Text = Loc.GetString("rmc-ui-link-discord-account-copied");
                _window.CopyButton.Disabled = true;
                _disableUntil = _timing.RealTime.Add(TimeSpan.FromSeconds(3));
            };

            var messageLink = _config.GetCVar(RMCCVars.RMCDiscordAccountLinkingMessageLink);
            if (string.IsNullOrEmpty(messageLink))
            {
                _window.LinkButton.Visible = false;
                _window.CopyButton.RemoveStyleClass("OpenRight");
            }
            else
            {
                _window.LinkButton.Visible = true;
                _window.LinkButton.OnPressed += _ => _uriOpener.OpenUri(messageLink);
                _window.CopyButton.AddStyleClass("OpenRight");
            }

            _window.OpenCentered();

            if (_code == default)
                _window.CopyButton.Disabled = true;

            _net.ClientSendMessage(new LinkAccountRequestMsg());
            return;
        }

        _window.Close();
        _window = null;
    }

    public void TogglePatronPerksWindow()
    {
        if (_patronPerksWindow == null)
        {
            _patronPerksWindow = new PatronPerksWindow();
            _patronPerksWindow.OnClose += () => _patronPerksWindow = null;

            UpdateExamples();

            for (var i = 0; i < _patronPerksWindow.Tabs.ChildCount; i++)
            {
                var child = _patronPerksWindow.Tabs.GetChild(i);
                if (!child.GetValue(TabVisibleProperty))
                    continue;

                _patronPerksWindow.Tabs.CurrentTab = i;
                break;
            }

            _patronPerksWindow.OpenCentered();
            return;
        }

        _patronPerksWindow.Close();
        _patronPerksWindow = null;
    }

    private void ChangeLobbyMessage(LineEditEventArgs args)
    {
        var text = args.Text;
        if (text.Length > SharedRMCLobbyMessage.CharacterLimit)
        {
            text = text[..SharedRMCLobbyMessage.CharacterLimit];
            _patronPerksWindow?.LobbyMessage.SetText(text, false);
        }

        _net.ClientSendMessage(new RMCChangeLobbyMessageMsg { Text = text });
    }

    private void ChangeMarineShoutout(LineEditEventArgs args)
    {
        var text = args.Text;
        if (text.Length > SharedRMCRoundEndShoutouts.CharacterLimit)
        {
            text = text[..SharedRMCRoundEndShoutouts.CharacterLimit];
            _patronPerksWindow?.LobbyMessage.SetText(text, false);
        }

        _net.ClientSendMessage(new RMCChangeMarineShoutoutMsg { Name = text });
        UpdateExamples();
    }

    private void ChangeXenoShoutout(LineEditEventArgs args)
    {
        var text = args.Text;
        if (text.Length > SharedRMCRoundEndShoutouts.CharacterLimit)
        {
            text = text[..SharedRMCRoundEndShoutouts.CharacterLimit];
            _patronPerksWindow?.LobbyMessage.SetText(text, false);
        }

        _net.ClientSendMessage(new RMCChangeXenoShoutoutMsg { Name = text });
        UpdateExamples();
    }

    private void OnGhostColorChanged(Color color)
    {
        if (_patronPerksWindow is not { IsOpen: true })
            return;

        _patronPerksWindow.GhostColorSaveButton.Disabled = false;
    }

    private void OnGhostColorClear(ButtonEventArgs args)
    {
        if (_patronPerksWindow is not { IsOpen: true })
            return;

        _patronPerksWindow.GhostColorSliders.Color = Color.White;
        _patronPerksWindow.GhostColorSaveButton.Disabled = true;
        _net.ClientSendMessage(new RMCClearGhostColorMsg());
    }

    private void OnGhostColorSave(ButtonEventArgs args)
    {
        if (_patronPerksWindow is not { IsOpen: true })
            return;

        _patronPerksWindow.GhostColorSaveButton.Disabled = true;
        _net.ClientSendMessage(new RMCChangeGhostColorMsg { Color = _patronPerksWindow.GhostColorSliders.Color });
    }

    private void UpdateExamples()
    {
        if (_patronPerksWindow == null)
            return;

        var marine = _patronPerksWindow.MarineShoutout.Text.Trim();
        _patronPerksWindow.MarineShoutoutExample.SetMarkupPermissive(string.IsNullOrWhiteSpace(marine)
            ? " "
            : $"{Loc.GetString("rmc-ui-shoutout-example")} {Loc.GetString("rmc-ui-shoutout-marine", ("name", marine))}");

        var xeno = _patronPerksWindow.XenoShoutout.Text.Trim();
        _patronPerksWindow.XenoShoutoutExample.SetMarkupPermissive(string.IsNullOrWhiteSpace(xeno)
            ? " "
            : $"{Loc.GetString("rmc-ui-shoutout-example")} {Loc.GetString("rmc-ui-shoutout-xeno", ("name", xeno))}");
    }

    public void OnSystemLoaded(LinkAccountSystem system)
    {
        system.LobbyMessageReceived += OnLobbyMessageReceived;
    }

    public void OnSystemUnloaded(LinkAccountSystem system)
    {
        system.LobbyMessageReceived -= OnLobbyMessageReceived;
    }

    public override void FrameUpdate(FrameEventArgs args)
    {
        if (_window == null)
            return;

        var time = _timing.RealTime;
        if (_disableUntil != default && time > _disableUntil)
        {
            _disableUntil = default;
            _window.CopyButton.Text = Loc.GetString("rmc-ui-link-discord-account-copy");
            _window.CopyButton.Disabled = false;
        }
    }
}
