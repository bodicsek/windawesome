
class DateTimeWidget
  include Windawesome::IFixedWidthWidget
  include System
  include System::Drawing
  include System::Windows::Forms
  include System::Linq

  attr_accessor :format_string, :background_color, :foreground_color, :prefix, :suffix, :click

  def initialize args
    args.each do |k,v|
      self.send("#{k}=", v) unless v.nil?
    end

    self.format_string = "ddd, d-MMM" if self.format_string.nil?
    self.background_color = Color.black if self.background_color.nil?
    self.foreground_color = Color.gold if self.foreground_color.nil?
    self.prefix = " " if self.prefix.nil?
    self.suffix = " " if self.suffix.nil?

    @update_timer = Timer.new
    @update_timer.interval = 30000
    @update_timer.tick do |s, ea|
      old_left = @label.left
      old_right = @label.right
      old_width = @label.width
      @label.text = self.prefix + DateTime.now.to_string(self.format_string) + self.suffix
      @label.width = TextRenderer.measure_text(@label.text, @label.font).width
      if old_width != @label.width
	self.reposition_controls old_left, old_right
	@bar.do_fixed_width_widget_width_changed self
      end
    end
  end
  
  def static_initialize_widget windawesome
  end

  def initialize_widget bar
    @bar = bar
    
    @label = bar.create_label self.prefix + DateTime.now.to_string(self.format_string) + self.suffix, 0
    
    @label.text_align = ContentAlignment.middle_center
    @label.back_color = self.background_color
    @label.fore_color = self.foreground_color
    @label.click.add self.click if self.click
    
    @update_timer.start
  end

  def get_initial_controls is_left
    @is_left = is_left          
    Enumerable.repeat @label, 1
  end

  def reposition_controls left, right
    @label.location = @is_left ? Point.new(left, 0) : Point.new(right - @label.width, 0)
  end

  def get_left
    @label.left
  end

  def get_right
    @label.right
  end

  def static_dispose
  end

  def dispose
  end

  def refresh
  end

end
