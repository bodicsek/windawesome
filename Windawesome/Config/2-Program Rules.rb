include Windawesome

config.program_rules =
  [
    ProgramRule.new(:display_name   => ".*Microsoft Visual Studio.*",
                    :rules          =>
                    [
                      ProgramRule::Rule.new(:workspace => 2)
                    ].to_clr_a(ProgramRule::Rule)),

    ProgramRule.new(:process_name => "emacs.*",
                    :rules        =>
                    [
                      ProgramRule::Rule.new(:workspace => 3),
                    ].to_clr_a(ProgramRule::Rule)),
    
    ProgramRule.new(:process_name => "firefox.*",
                    :rules        =>
                    [
                      ProgramRule::Rule.new(:workspace => 4)
                    ].to_clr_a(ProgramRule::Rule)),
    
    ProgramRule.new(:style_contains => NativeMethods::WS.WS_POPUP,
                    :is_managed     => false),
    
    ProgramRule.new(:is_managed => true)
    
  ].to_clr_seq(ProgramRule)
