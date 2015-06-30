include System::Drawing
include System::Collections::Generic
include Windawesome
include Windawesome::Widgets
include Windawesome::Layouts
include Windawesome::Plugins

class Object
  def to_clr_seq(type = Object)
    System::Linq::Enumerable.method(:of_type).of(type).call(self.to_a)
  end
  def to_clr_a(type = Object)
    System::Linq::Enumerable.method(:to_array).of(type).call(self.to_clr_seq)
  end
end

config.window_border_width = 1
config.window_padded_border_width = 0
config.check_for_updates = false

config.bars =
  [
    Bar.new(windawesome.monitors[0],
            
            [
              
              WorkspacesWidget.new([Color.light_sea_green, Color.yellow, Color.yellow, Color.yellow, Color.yellow].to_clr_seq(Color),
                                   [Color.black          , Color.black , Color.black , Color.black , Color.black ].to_clr_seq(Color),
                                   Color.dark_orange,
                                   Color.black,
                                   Color.light_sea_green,
                                   Color.black,
                                   Color.black),
                           
              LayoutWidget.new(Color.black, Color.gold)
              
            ].to_clr_seq(IFixedWidthWidget),
                         
            [
              
              SystemTrayWidget.new(true)
              
            ].to_clr_seq(IFixedWidthWidget),
            
            [
              
              ApplicationTabsWidget.new(false, Color.light_sea_green, Color.black, Color.dark_orange, Color.black)
              
            ].to_clr_seq(ISpanWidget),
            20,
            Font.new("Consolas", 11),
            Color.black)
  ].to_clr_seq(IBar)

config.workspaces =
  [
    
    Workspace.new(windawesome.monitors[0], FloatingLayout.new, [config.bars[0]].to_clr_seq(IBar))
    
  ].to_clr_seq(Workspace)

config.starting_workspaces =
  [
  
    config.workspaces[0]
  
  ].to_clr_seq(Workspace)

config.plugins =
  [

    ShortcutsManager.new

  ].to_clr_seq(IPlugin)
