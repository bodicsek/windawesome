include Windawesome
include Windawesome::Plugins

def subscribe(modifiers, key)
  ShortcutsManager.subscribe modifiers, key, lambda {
    ret = yield
    return true if ret == nil
    ret
  }
end

flashing_window = nil
previous_workspace = config.workspaces[0]

def get_current_workspace_managed_window
  _, window, _ = windawesome.try_get_managed_window_for_workspace NativeMethods.get_foreground_window, windawesome.current_workspace
  window
end

Windawesome::Windawesome.window_flashing { |hWnd, l| flashing_window = hWnd }

Workspace.workspace_deactivated { |ws| previous_workspace = ws }

modifiers = ShortcutsManager::KeyModifiers
key = System::Windows::Forms::Keys

# quit Windawesome
subscribe modifiers.LWin | modifiers.Shift, key.Q do
  windawesome.quit
end

# refresh viewed sizes
subscribe modifiers.LWin, key.N do
  windawesome.refresh_windawesome
end

# quit application
subscribe modifiers.LWin | modifiers.Shift, key.C do
  hWnd = NativeMethods.get_foreground_window
  Utilities.quit_application hWnd if NativeMethods.get_window_class_name(hWnd) != "WorkerW"
end

# kill focused window
subscribe modifiers.LWin | modifiers.Control | modifiers.Shift, key.C do
  windawesome.remove_application_from_workspace NativeMethods.get_foreground_window
end

# start terminal
subscribe modifiers.LWin | modifiers.Shift , key.Return do
  windawesome.run_application "C:\\tools\\cmder\\Cmder.exe"
end

# swap focused window with master window
subscribe modifiers.LWin, key.Return do
  window = get_current_workspace_managed_window
  windawesome.current_workspace.shift_window_to_main_position window if window
end

# next layout
subscribe modifiers.LWin, key.Space do
  if windawesome.current_workspace.layout.layout_name == "Tile"
    windawesome.current_workspace.change_layout FloatingLayout.new
  elsif windawesome.current_workspace.layout.layout_name == "Floating"
    windawesome.current_workspace.change_layout FullScreenLayout.new
  elsif windawesome.current_workspace.layout.layout_name == "Full Screen"
    windawesome.current_workspace.change_layout TileLayout.new
  end
end

# reset current workspace layout to default
subscribe modifiers.LWin | modifiers.Shift, key.Space do
  windawesome.current_workspace.change_layout FloatingLayout.new
end

# focus next window (should subscribe LWin + Tab)
def focus_next_window
  window = get_current_workspace_managed_window
  if window
    next_window = windawesome.current_workspace.get_next_window window
    windawesome.switch_to_application next_window.hWnd if next_window
  elsif windawesome.current_workspace.get_windows_count > 0
    windawesome.switch_to_application windawesome.current_workspace.get_windows.first.value.hWnd
  end
end

subscribe modifiers.LWin, key.J do
  focus_next_window
end

subscribe modifiers.LWin, key.Tab do
  focus_next_window
end


# swap with next window
subscribe modifiers.LWin | modifiers.Shift, key.J do
  window = get_current_workspace_managed_window
  windawesome.current_workspace.shift_window_forward window if window
end

# focus previous window (should subscribe LWin + Shift + Tab)
def focus_previous_window
    window = get_current_workspace_managed_window
  if window
    previous_window = windawesome.current_workspace.get_previous_window window
    windawesome.switch_to_application previous_window.hWnd if previous_window
  elsif windawesome.current_workspace.get_windows_count > 0
    windawesome.switch_to_application windawesome.current_workspace.get_windows.first.value.hWnd
  end
end

subscribe modifiers.LWin, key.K do
  focus_previous_window
end

subscribe modifiers.LWin | modifiers.Shift, key.Tab do
  focus_previous_window
end

# swap with previous window
subscribe modifiers.LWin | modifiers.Shift, key.K do
  window = get_current_workspace_managed_window
  windawesome.current_workspace.shift_window_backwards window if window
end

# shrink master area (yet with shift)
subscribe modifiers.LWin | modifiers.Shift, key.H do
  windawesome.current_workspace.layout.add_to_master_area_factor -0.05 if windawesome.current_workspace.layout.layout_name == "Tile"
end

# expand master area (yet with shift)
subscribe modifiers.LWin | modifiers.Shift, key.L do
  windawesome.current_workspace.layout.add_to_master_area_factor if windawesome.current_workspace.layout.layout_name == "Tile"
end

(1 .. config.workspaces.length).each do |i|
  k = eval("key.D" + i.to_s)

  # switch to workspace k
  subscribe modifiers.LWin, k do
    windawesome.switch_to_workspace i
  end

  # move application to workspace k
  subscribe modifiers.LWin | modifiers.Shift, k do
    windawesome.change_application_to_workspace NativeMethods.get_foreground_window, i
  end
end

def add_to_master_area_windows_count(num)
  if windawesome.current_workspace.layout.layout_name == "Tile"
    windawesome.current_workspace.layout.add_to_master_area_windows_count num
  end
end

# more master windows
subscribe modifiers.LWin, key.Oemcomma do
  add_to_master_area_windows_count 1
end

# fewer master windows
subscribe modifiers.LWin, key.OemPeriod do
  add_to_master_area_windows_count -1
end





# switch to flashing window
subscribe modifiers.LWin, key.U do
  windawesome.switch_to_application flashing_window if flashing_window
end

# toggle window floating
subscribe modifiers.Control | modifiers.LWin | modifiers.Shift, key.F do
  windawesome.toggle_window_floating NativeMethods.get_foreground_window
end

# toggle window titlebar
subscribe modifiers.LWin | modifiers.Shift, key.D do
  windawesome.toggle_show_hide_window_titlebar NativeMethods.get_foreground_window
end

# toggle Windows taskbar
subscribe modifiers.LWin | modifiers.Control, key.Space do
  windawesome.toggle_taskbar_visibility
end

# toggle window border
subscribe modifiers.LWin | modifiers.Shift, key.B do
  windawesome.toggle_show_hide_window_border NativeMethods.get_foreground_window
end

# toggle window menu
subscribe modifiers.Control | modifiers.LWin | modifiers.Shift, key.M do
  windawesome.toggle_show_hide_window_menu NativeMethods.get_foreground_window
end

# Layout stuff

# window position stuff

# Tile Layout stuff

subscribe modifiers.LWin | modifiers.Shift, key.L do
  windawesome.current_workspace.layout.toggle_layout_axis if windawesome.current_workspace.layout.layout_name == "Tile"
end

subscribe modifiers.LWin | modifiers.Shift, key.S do
  windawesome.current_workspace.layout.toggle_stack_area_axis if windawesome.current_workspace.layout.layout_name == "Tile"
end

subscribe modifiers.Control | modifiers.LWin | modifiers.Shift, key.S do
  windawesome.current_workspace.layout.toggle_master_area_axis if windawesome.current_workspace.layout.layout_name == "Tile"
end


# Workspaces stuff

