using Godot;
using System;

public partial class Player : CharacterBody2D
{
	// Get the gravity from the project settings to be synced with CharacterBody2D nodes.
	public float gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();

	/// <summary>
	/// Used to determine the length of the time you're dashing, currently set to 0.3 in the editor but you can change it to any time you want.
	/// </summary>
	[Export]
	public Timer _DashTimer;

	[Export(PropertyHint.Range, "100,300")]
	public float _Speed = 300.0f;

	[Export(PropertyHint.Range, "-400,-100")]
	public float _JumpVelocity = -400.0f;

	[Export(PropertyHint.Range, "100, 300")]
	public float _SprintSpeedAdder;

	[Export(PropertyHint.Range, "400, 500")]
	public float _DashSpeedAdder;

	private float _MoveDirection, _DashDirection;

	/// <summary>
	/// Used when player suddenly move to opposite direction or stopped inputting while dashing, if set to true this will freeze the character until the x velocity is approximately 0
	/// </summary>
	private bool _DashCooldown;

	public override void _Ready()
	{
		_DashTimer.Timeout += _DashTimer_Timeout;
	}

	/// <summary>
	/// This is not used on this demo but you can put whatever here to trigger your code when the player has finished dashing
	/// </summary>
	private void _DashTimer_Timeout()
	{

    }

	/// <summary>
	/// Draw a trailing line based on the current speed, you can use <see cref="Line2D"/> for more advanced use.
	/// </summary>
	public override void _Draw()
	{
		//Early return if any of the requirement is met, this will remove the old trailing line by not doing anything
		if (Mathf.IsZeroApprox(Velocity.X) || (_DashTimer.TimeLeft <= 0 && !Input.IsActionPressed("Sprint") && !_DashCooldown))
			return;

		var localPos = ToLocal(GlobalPosition);

		var targetDirection = _DashTimer.TimeLeft > 0 || _DashCooldown ? _DashDirection : _MoveDirection;

		float offsetY = (float)GD.RandRange(-20.0, 20.0);
		float offsetX;
		float size;

		if (_DashTimer.TimeLeft > 0)
		{
			offsetX = (float)GD.RandRange(30.0, Mathf.Abs(Velocity.X) / 5);
			size = (float)GD.RandRange(1.0, 2.0);
		}
		else
		{
			offsetX = (float)GD.RandRange(30.0, Mathf.Abs(Velocity.X) / 6);
			size = (float)GD.RandRange(0.5, 1.0);
		}
		
		//We swapped the target direction so its spawned the opposite side the player is moving towards
		var targetPos = new Vector2((localPos.X + offsetX) * -targetDirection, localPos.Y + offsetY);

		DrawLine(localPos with { Y = targetPos.Y}, targetPos, Colors.Gray, size);

	}

	#region Character Movement
	public override void _PhysicsProcess(double delta)
	{
		Vector2 velocity = Velocity;

		// Add the gravity.
		if (!IsOnFloor())
			velocity.Y += gravity * (float)delta;

		// Handle Jump.
		if (Input.IsActionJustPressed("ui_accept") && IsOnFloor())
			velocity.Y = _JumpVelocity;

		// Get the input direction and handle the movement/deceleration.
		velocity = Movement(velocity);

		if (_DashCooldown && Mathf.IsZeroApprox(Velocity.X))
		{
			_DashDirection = 0;
			_DashCooldown = false;
		}

		Velocity = velocity;

		//Draw the trailing line, if any of the requirement does not fit in the _Draw method, then it will remove the old trailing line
		QueueRedraw();

		MoveAndSlide();
	}

	private Vector2 Movement(Vector2 velocity)
	{
		Vector2 direction = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
		_MoveDirection = direction.X;

		if (direction != Vector2.Zero && !_DashCooldown)
		{
			//If player has dashed before, and suddenly move in another direction. Then "freeze" the character until it stop moving
			if (_DashTimer.TimeLeft > 0 && (_MoveDirection > _DashDirection || _MoveDirection < _DashDirection))
			{
				_DashTimer.Stop();
				_DashCooldown = true;
			}

			if (!_DashCooldown)
			{
				if (Input.IsActionJustPressed("Dash") && _DashTimer.TimeLeft <= 0)
				{
					_DashTimer.Start();
					_DashDirection = _MoveDirection;
				}

				velocity.X = _MoveDirection * _Speed;

				if (Input.IsActionPressed("Sprint"))
					velocity.X += _MoveDirection * _SprintSpeedAdder;

				if (_DashTimer.TimeLeft > 0)
					velocity.X += _MoveDirection * _DashSpeedAdder;
			}

			else
				velocity = LerpXVelocity(velocity);

		}

		//If player has dashed before, and suddenly stops pressing movement input. Then "freeze" the character until it stop moving
		else if (Mathf.IsZeroApprox(direction.X) && _DashTimer.TimeLeft > 0)
		{
			_DashTimer.Stop();
			_DashCooldown = true;
			velocity = LerpXVelocity(velocity);
		}

		else
			velocity = LerpXVelocity(velocity);

		return velocity;
	}

	/// <summary>
	/// Used to smoothly stops the character
	/// </summary>
	/// <param name="velocity"></param>
	/// <returns></returns>
	private Vector2 LerpXVelocity(Vector2 velocity)
	{
		velocity.X = Mathf.MoveToward(Velocity.X, 0, _Speed / (_DashCooldown ? 10 : 5));
		return velocity;
	}
	#endregion
	
}
