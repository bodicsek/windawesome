include System::Drawing
include Windawesome
include Windawesome::Widgets
include Windawesome::Layouts
include Windawesome::Plugins

config.window_border_width = 1
config.window_padded_border_width = 0
config.check_for_updates = false

config.bars =
  [
    Bar.new(:monitor => windawesome.monitors[0],

            :left_aligned_widgets =>
            [
              LayoutWidget.new(:background_color => Color.black,
                               :foreground_color => Color.gold),

              WorkspacesWidget.new(:normal_foreground_color               => Color.light_sea_green,
                                   :normal_background_color               => Color.black,
                                   :highlighted_foreground_color          => Color.dark_orange,
                                   :highlighted_background_color          => Color.black,
                                   :highlighted_inactive_foreground_color => Color.light_sea_green,
                                   :highlighted_inactive_background_color => Color.black,
                                   :flashing_foreground_color             => Color.red,
                                   :flashing_background_color             => Color.black,
                                   :flash_workspaces                      => true),
              
              SeparatorWidget.new(:background_color => Color.black,
                                  :foreground_color => Color.gold)
              
            ].to_clr_a(IFixedWidthWidget),

            :right_aligned_widgets =>
            [
              SeparatorWidget.new(:background_color => Color.black,
                                  :foreground_color => Color.gold),
              
              SystemTrayWidget.new(:show_full_system_tray => true),
              
              DateTimeWidget.new(:format_string => "ddd, d-MMM",
                                 :background_color => Color.black,
                                 :foreground_color => Color.gold),
              
              DateTimeWidget.new(:prefix => "",
                                 :format_string => "h:mm tt",
                                 :background_color => Color.black,
                                 :foreground_color => Color.gold)
              
            ].to_clr_a(IFixedWidthWidget),

            :middle_aligned_widgets =>
            [
              
              ApplicationTabsWidget.new(:show_single_application_tab  => false,
                                        :normal_foreground_color      => Color.light_sea_green,
                                        :normal_background_color      => Color.black,
                                        :highlighted_foreground_color => Color.dark_orange,
                                        :highlighted_background_color => Color.black)
              
            ].to_clr_a(ISpanWidget),
            
            :bar_height => 20,
            
            :font => Font.new("Consolas", 11),
            
            :background_color => Color.black)
        
  ].to_clr_a(IBar)

config.workspaces =
  [
    
    Workspace.new(:monitor                   => windawesome.monitors[0],
                  :layout                    => TileLayout.new,
                  :bars_at_top               => [config.bars[0]].to_clr_a(IBar),
                  :reposition_on_switched_to => true,
                  :name                      => "1"),
    
    Workspace.new(:monitor                   => windawesome.monitors[0],
                  :layout                    => FullScreenLayout.new,
                  :bars_at_top               => [config.bars[0]].to_clr_a(IBar),
                  :reposition_on_switched_to => true,
                  :name                      => "2"),

    Workspace.new(:monitor                   => windawesome.monitors[0],
                  :layout                    => TileLayout.new,
                  :bars_at_top               => [config.bars[0]].to_clr_a(IBar),
                  :reposition_on_switched_to => true,
                  :name                      => "3"),
    
    Workspace.new(:monitor                   => windawesome.monitors[0],
                  :layout                    => FullScreenLayout.new,
                  :bars_at_top               => [config.bars[0]].to_clr_a(IBar),
                  :reposition_on_switched_to => true,
                  :name                      => "4")
        
  ].to_clr_a(Workspace)

config.starting_workspaces =
  [
  
    config.workspaces[0]
  
  ].to_clr_seq(Workspace)

config.plugins =
  [

    ShortcutsManager.new

  ].to_clr_seq(IPlugin)
