using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using WeightlessCore.Simulation;
using WeightlessCore.Utility;
using System;
using Microsoft.Xna.Framework.Input;

namespace WeightlessCore
{
	/// <summary>
	/// This is the main type for your game.
	/// </summary>
	public class WeightlessGame : Game
	{
		#region Constants

		private const float CollisionBias = 0.01f;

		#endregion

		#region Fields

		private readonly GraphicsDeviceManager _graphics;

		private KeyboardState _oldKeyboardState;

		private Random _random;

		private PointLight _light;
		private Vector3 _cameraPosition;
		private Plane[] _environmentBoundaries;

		private RigidCuboid[] _rigidCuboids;
		
		private Matrix _viewMatrix, _projectionMatrix;

		private Effect _singleColourEffect;

		private Model _environmentModel;

		private Vector3 _gravity;

		#endregion

		#region Constructors

		public WeightlessGame()
		{
			_graphics = new GraphicsDeviceManager(this) { PreferMultiSampling = true };

			Content.RootDirectory = "Content";
		}

		#endregion

		#region Private methods

		#region Content loading

		private void LoadRigidCuboid()
		{
			RigidCuboid.Model = Content.Load<Model>("Models/RigidCuboid");

			foreach (ModelMesh mesh in RigidCuboid.Model.Meshes)
			{
				foreach (ModelMeshPart part in mesh.MeshParts)
				{
					part.Effect = _singleColourEffect;
				}
			}
		}

		private void LoadEnvironment()
		{
			_environmentModel = Content.Load<Model>("Models/Environment");

			foreach (ModelMesh mesh in _environmentModel.Meshes)
			{
				foreach (ModelMeshPart part in mesh.MeshParts)
				{
					part.Effect = _singleColourEffect;
				}
			}
		}

		#endregion

		#region Content drawing

		private void DrawScene()
		{
			// Environment
			GraphicsDevice.DepthStencilState = DepthStencilState.None;

			foreach (ModelMesh mesh in _environmentModel.Meshes)
			{
				foreach (Effect effect in mesh.Effects)
				{
					effect.CurrentTechnique = effect.Techniques["SingleColourTechnique"];

					effect.Parameters["World"].SetValue(Matrix.Identity);
					effect.Parameters["WorldViewProjection"].SetValue(_viewMatrix*_projectionMatrix);
					effect.Parameters["LightPower"].SetValue(_light.Power);
					effect.Parameters["AmbientLightPower"].SetValue(_light.AmbientPower);
					effect.Parameters["LightAttenuation"].SetValue(_light.Attenuation);
					effect.Parameters["SpecularExponent"].SetValue(_light.SpecularExponent);
					effect.Parameters["CameraPosition"].SetValue(_cameraPosition);
					effect.Parameters["LightPosition"].SetValue(_light.Position);
					effect.Parameters["BaseColour"].SetValue(Color.LightGray.ToVector3());
					effect.Parameters["SpecularColour"].SetValue(Vector3.Zero);
				}

				mesh.Draw();
			}

			GraphicsDevice.DepthStencilState = DepthStencilState.Default;

			// Rigid cuboids
			foreach (RigidCuboid rigidCuboid in _rigidCuboids)
			{
				Matrix rigidCuboidWorldMatrix = rigidCuboid.ScaleMatrix*rigidCuboid.State.Orientation*Matrix.CreateTranslation(rigidCuboid.State.Position);

				foreach (ModelMesh mesh in RigidCuboid.Model.Meshes)
				{
					foreach (Effect effect in mesh.Effects)
					{
						effect.CurrentTechnique = effect.Techniques["SingleColourTechnique"];

						effect.Parameters["World"].SetValue(rigidCuboidWorldMatrix);
						effect.Parameters["WorldViewProjection"].SetValue(rigidCuboidWorldMatrix*_viewMatrix*_projectionMatrix);
						effect.Parameters["LightPower"].SetValue(_light.Power);
						effect.Parameters["AmbientLightPower"].SetValue(_light.AmbientPower);
						effect.Parameters["LightAttenuation"].SetValue(_light.Attenuation);
						effect.Parameters["SpecularExponent"].SetValue(_light.SpecularExponent);
						effect.Parameters["CameraPosition"].SetValue(_cameraPosition);
						effect.Parameters["LightPosition"].SetValue(_light.Position);
						effect.Parameters["BaseColour"].SetValue(rigidCuboid.Colour.ToVector3());
						effect.Parameters["SpecularColour"].SetValue(Color.LightGray.ToVector3());
					}

					mesh.Draw();
				}
			}
		}

		#endregion

		#region Misc.

		private RigidCuboid GetRandomRigidCuboid()
		{
			Vector3 rigidCuboidDimensions = new Vector3((float)(0.05f + 0.95f*_random.NextDouble()), (float)(0.05f + 0.95f*_random.NextDouble()), (float)(0.05f + 0.95f*_random.NextDouble()));
			float rigidCuboidMass = (float)(1 + 4*_random.NextDouble())*rigidCuboidDimensions.X*rigidCuboidDimensions.Y*rigidCuboidDimensions.Z;
			Matrix rigidCuboidInverseBodyInertiaTensor = Matrix.Invert(new Matrix(new Vector4(rigidCuboidMass/12*(rigidCuboidDimensions.Y*rigidCuboidDimensions.Y + rigidCuboidDimensions.Z*rigidCuboidDimensions.Z), 0, 0, 0), new Vector4(0, rigidCuboidMass/12*(rigidCuboidDimensions.X*rigidCuboidDimensions.X + rigidCuboidDimensions.Z*rigidCuboidDimensions.Z), 0, 0), new Vector4(0, 0, rigidCuboidMass/12*(rigidCuboidDimensions.X*rigidCuboidDimensions.X + rigidCuboidDimensions.Y*rigidCuboidDimensions.Y), 0), new Vector4(0, 0, 0, 1)));

			RigidCuboid result = new RigidCuboid()
			{
				Colour = new Color(new Vector3((float)(0.2f + 0.6f*_random.NextDouble()), (float)(0.2f + 0.6f*_random.NextDouble()), (float)(0.2f + 0.6f*_random.NextDouble()))),
				ScaleMatrix = Matrix.CreateScale(rigidCuboidDimensions),
				Mass = rigidCuboidMass,
				InverseBodyInertiaTensor = rigidCuboidInverseBodyInertiaTensor,
				BoundingVertices = new Vector3[]
				{
					rigidCuboidDimensions*new Vector3(-1, -1, -1)/2,
					rigidCuboidDimensions*new Vector3(1, -1, -1)/2,
					rigidCuboidDimensions*new Vector3(-1, -1, 1)/2,
					rigidCuboidDimensions*new Vector3(1, -1, 1)/2,
					rigidCuboidDimensions*new Vector3(-1, 1, -1)/2,
					rigidCuboidDimensions*new Vector3(1, 1, -1)/2,
					rigidCuboidDimensions*new Vector3(-1, 1, 1)/2,
					rigidCuboidDimensions*new Vector3(1, 1, 1)/2
				},
				State = new RigidBodyState()
				{
					Position = new Vector3((float)(-1 + 2*_random.NextDouble()), (float)(-1 + 2*_random.NextDouble()), (float)(-4 + 6*_random.NextDouble())),
					Velocity = new Vector3((float)(-5 + 10*_random.NextDouble()), (float)(-5 + 10*_random.NextDouble()), (float)(-5 + 10*_random.NextDouble())),
					Orientation = Matrix.Identity,
					AngularMomentum = 0.2f*rigidCuboidMass*new Vector3((float)(-1 + 2*_random.NextDouble()), (float)(-1 + 2*_random.NextDouble()), (float)(-1 + 2*_random.NextDouble()))
				}
			};

			return result;
		}

		private void ProcessInput()
		{
			KeyboardState newKeyboardState = Keyboard.GetState();

			if (_oldKeyboardState.IsKeyDown(Keys.R) && newKeyboardState.IsKeyUp(Keys.R))
			{
				_rigidCuboids = new RigidCuboid[]
				{
					GetRandomRigidCuboid(),
					GetRandomRigidCuboid(),
					GetRandomRigidCuboid(),
					GetRandomRigidCuboid(),
					GetRandomRigidCuboid()
				};
			}

			_gravity = 3*new Vector3(newKeyboardState.IsKeyDown(Keys.Right) ? -1 : newKeyboardState.IsKeyDown(Keys.Left) ? 1 : 0, newKeyboardState.IsKeyDown(Keys.Up) ? -1 : newKeyboardState.IsKeyDown(Keys.Down) ? 1 : 0, newKeyboardState.IsKeyDown(Keys.S) ? -1 : newKeyboardState.IsKeyDown(Keys.W) ? 1 : 0);

			_oldKeyboardState = newKeyboardState;
		}

		private void UpdatePhysics(float timeDelta)
		{
			// Rigid cuboids
			foreach (RigidCuboid rigidCuboid in _rigidCuboids)
			{
				Vector3 rigidCuboidForce = Vector3.Zero;
				Vector3 rigidCuboidTorque = Vector3.Zero;

				foreach (Plane environmentBoundary in _environmentBoundaries)
				{
					// Vertex-environment interactions
					foreach (Vector3 vertexPosition in rigidCuboid.BoundingVertices)
					{
						Vector3 vertexLocalPosition = Vector3.Transform(vertexPosition, rigidCuboid.State.Orientation);

						float vertexPenetrationDepth = CollisionBias - Globals.PointPlaneDistance(rigidCuboid.State.Position + vertexLocalPosition, environmentBoundary);
						if (vertexPenetrationDepth >= 0)
						{
							Vector3 vertexVelocity = rigidCuboid.State.Velocity + Vector3.Cross(rigidCuboid.State.AngularVelocity, vertexLocalPosition);
							Vector3 vertexContactForce = rigidCuboid.Mass*(RigidCuboid.ReactionForceMultiplier*environmentBoundary.Normal*vertexPenetrationDepth - RigidCuboid.FrictionForceMultiplier*vertexVelocity);

							rigidCuboidForce += vertexContactForce;
							rigidCuboidTorque += Vector3.Cross(vertexLocalPosition, vertexContactForce);
						}
					}
				}

				rigidCuboidForce += rigidCuboid.Mass*_gravity;
				//rigidCuboidTorque -= 0.1f*rigidCuboid.State.AngularMomentum;

				rigidCuboid.ApplyPhysics(rigidCuboidForce, rigidCuboidTorque, timeDelta);
			}
		}

		#endregion

		#endregion

		#region Game overrides

		/// <summary>
		/// Allows the game to perform any initialization it needs to before starting to run.
		/// This is where it can query for any required services and load any non-graphic
		/// related content.  Calling base.Initialize will enumerate through any components
		/// and initialize them as well.
		/// </summary>
		protected override void Initialize()
		{
			_oldKeyboardState = Keyboard.GetState();

			_random = new Random();

			_light = new PointLight(new Vector3(0, 1.8f, 0), 1, 0.2f, 10, 64);

			_cameraPosition = new Vector3(0, 0, 5);

			_environmentBoundaries = new Plane[]
			{
				new Plane(Vector3.UnitY, -2),
				new Plane(-Vector3.UnitY, -2),
				new Plane(Vector3.UnitX, -2),
				new Plane(-Vector3.UnitX, -2),
				new Plane(Vector3.UnitZ, -5),
				new Plane(-Vector3.UnitZ, -4),
				new Plane(Vector3.Normalize(new Vector3(-1, -1, 0)), -2.298f),
				new Plane(Vector3.Normalize(new Vector3(1, -1, 0)), -2.298f),
				new Plane(Vector3.Normalize(new Vector3(-1, 1, 0)), -2.298f),
				new Plane(Vector3.Normalize(new Vector3(1, 1, 0)), -2.298f)
			};

			_rigidCuboids = new RigidCuboid[]
			{
				GetRandomRigidCuboid(),
				GetRandomRigidCuboid(),
				GetRandomRigidCuboid(),
				GetRandomRigidCuboid(),
				GetRandomRigidCuboid()
			};

			_viewMatrix = Matrix.CreateLookAt(_cameraPosition, new Vector3(0, 0, 0), new Vector3(0, 1, 0));
			_projectionMatrix = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(60), GraphicsDevice.Viewport.AspectRatio, 0.1f, 100);

			base.Initialize();
		}

		/// <summary>
		/// LoadContent will be called once per game and is the place to load
		/// all of your content.
		/// </summary>
		protected override void LoadContent()
		{
			_singleColourEffect = Content.Load<Effect>("Effects/SingleColourEffect");

			LoadRigidCuboid();
			LoadEnvironment();
		}

		/// <summary>
		/// UnloadContent will be called once per game and is the place to unload
		/// game-specific content.
		/// </summary>
		protected override void UnloadContent()
		{
			// TODO: Unload any non ContentManager content here
		}

		/// <summary>
		/// Allows the game to run logic such as updating the world,
		/// checking for collisions, gathering input, and playing audio.
		/// </summary>
		/// <param name="gameTime">Provides a snapshot of timing values.</param>
		protected override void Update(GameTime gameTime)
		{
			ProcessInput();

			UpdatePhysics(gameTime.ElapsedGameTime.Milliseconds/1000.0f);
			
			base.Update(gameTime);
		}

		/// <summary>
		/// This is called when the game should draw itself.
		/// </summary>
		/// <param name="gameTime">Provides a snapshot of timing values.</param>
		protected override void Draw(GameTime gameTime)
		{
			// PASS 1: Draw the scene
			GraphicsDevice.Clear(Color.Black);

				DrawScene();

			base.Draw(gameTime);
		}

		#endregion
	}
}
