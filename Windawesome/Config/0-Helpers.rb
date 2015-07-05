include Windawesome
include Windawesome::Widgets

class Object
  def to_clr_seq(type = Object)
    System::Linq::Enumerable.method(:of_type).of(type).call(self.to_a)
  end
  def to_clr_a(type = Object)
    System::Linq::Enumerable.method(:to_array).of(type).call(self.to_clr_seq(type))
  end
end

module HashInit
  def initialize args
    args.each do |k,v|
      self.send("#{k}=", v) unless v.nil?
    end
  end
end

class Bar
  include HashInit
end

class Workspace
  include HashInit  
end

class WorkspacesWidget
  include HashInit
end

class LayoutWidget
  include HashInit
end

class ApplicationTabsWidget
  include HashInit
end

class SystemTrayWidget
  include HashInit
end

class SeparatorWidget
  include HashInit
end
