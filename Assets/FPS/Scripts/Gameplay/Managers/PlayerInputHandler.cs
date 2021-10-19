using Unity.FPS.Game;
using UnityEngine;

namespace Unity.FPS.Gameplay {
	public class PlayerInputHandler : MonoBehaviour {
		public float LookSensitivity = 1f;
		public float WebglLookSensitivityMultiplier = 0.25f;

		[Tooltip("Limit to consider an input when using a trigger on a controller")]
		public float TriggerAxisThreshold = 0.4f;

		public bool InvertYAxis = false;
		public bool InvertXAxis = false;

		// member variables
		PlayerCharacterController m_PlayerCharacterController;
		bool m_FireInputWasHeld;

		void Start() {
			m_PlayerCharacterController = GetComponent<PlayerCharacterController>();

			Cursor.lockState = CursorLockMode.Locked;
			Cursor.visible = false;
		}

		void LateUpdate() {
			m_FireInputWasHeld = GetFireInputHeld();
		}

		public bool CanProcessInput() {
			return Cursor.lockState == CursorLockMode.Locked;
		}


		//
		// FUNCTIONS TO PROCESS GAME INPUTS
		//
		public Vector3 GetMoveInput() {
			if (CanProcessInput()) {
				Vector3 move = new Vector3(Input.GetAxisRaw(GameConstants.k_AxisNameHorizontal), 0f, Input.GetAxisRaw(GameConstants.k_AxisNameVertical));

				// constrain move input to a maximum magnitude of 1, otherwise diagonal movement might exceed the max move speed defined
				move = Vector3.ClampMagnitude(move, 1);

				return move;
			} else {
				return Vector3.zero;
			}
		}
		public float GetLookInputsHorizontal() {
			return GetMouseOrStickLookAxis(GameConstants.k_MouseAxisNameHorizontal, GameConstants.k_AxisNameJoystickLookHorizontal);
		}
		public float GetLookInputsVertical() {
			return GetMouseOrStickLookAxis(GameConstants.k_MouseAxisNameVertical, GameConstants.k_AxisNameJoystickLookVertical);
		}
		public bool GetJumpInputDown() {
			if (CanProcessInput()) {
				return Input.GetButtonDown(GameConstants.k_ButtonNameJump);
			} else {
				return false;
			}
		}
		public bool GetJumpInputHeld() {
			if (CanProcessInput()) {
				return Input.GetButton(GameConstants.k_ButtonNameJump);
			} else {
				return false;
			}
		}
		public bool GetFireInputDown() {
			return GetFireInputHeld() && !m_FireInputWasHeld;
		}
		public bool GetFireInputReleased() {
			return !GetFireInputHeld() && m_FireInputWasHeld;
		}
		public bool GetFireInputHeld() {
			if (CanProcessInput()) {
				return Input.GetButton(GameConstants.k_ButtonNameFire);
			} else {
				return false;
			}
		}
		public bool GetAimInputHeld() {
			if (CanProcessInput()) {
				return Input.GetButton(GameConstants.k_ButtonNameAim);
			} else {
				return false;
			}
		}
		public bool GetSprintInputHeld() {
			if (CanProcessInput()) {
				return Input.GetButton(GameConstants.k_ButtonNameSprint);
			} else {
				return false;
			}
		}
		public bool GetCrouchInputDown() {
			if (CanProcessInput()) {
				return Input.GetButtonDown(GameConstants.k_ButtonNameCrouch);
			} else {
				return false;
			}
		}
		public bool GetCrouchInputReleased() {
			if (CanProcessInput()) {
				return Input.GetButtonUp(GameConstants.k_ButtonNameCrouch);
			} else {
				return false;
			}
		}
		public bool GetReloadButtonDown() {
			if (CanProcessInput()) {
				return Input.GetButtonDown(GameConstants.k_ButtonReload);
			} else {
				return false;
			}
		}
		public int GetSwitchWeaponInput() {
			if (CanProcessInput()) {
				if (Input.GetAxis(GameConstants.k_ButtonNameSwitchWeapon) > 0f)
					return -1;
				else if (Input.GetAxis(GameConstants.k_ButtonNameSwitchWeapon) < 0f)
					return 1;
				else if (Input.GetAxis(GameConstants.k_ButtonNameNextWeapon) > 0f)
					return -1;
				else if (Input.GetAxis(GameConstants.k_ButtonNameNextWeapon) < 0f)
					return 1;
			} else {
				return 0;
			}
		}
		public int GetSelectWeaponInput() {
			if (CanProcessInput()) {
				if (Input.GetKeyDown(KeyCode.Alpha1))
					return 1;
				else if (Input.GetKeyDown(KeyCode.Alpha2))
					return 2;
				else if (Input.GetKeyDown(KeyCode.Alpha3))
					return 3;
				else if (Input.GetKeyDown(KeyCode.Alpha4))
					return 4;
				else if (Input.GetKeyDown(KeyCode.Alpha5))
					return 5;
				else if (Input.GetKeyDown(KeyCode.Alpha6))
					return 6;
				else if (Input.GetKeyDown(KeyCode.Alpha7))
					return 7;
				else if (Input.GetKeyDown(KeyCode.Alpha8))
					return 8;
				else if (Input.GetKeyDown(KeyCode.Alpha9))
					return 9;
				else
					return 0;
			} else {
				return 0;
			}
		}
		//
		// END
		//


		float GetMouseOrStickLookAxis(string mouseInputName) {
			if (CanProcessInput()) {
				float i = Input.GetAxisRaw(mouseInputName);

				// apply invert to input
				if (InvertYAxis) 
					i *= -1f;

				// apply sensitivity to input
				i *= LookSensitivity;

				// reduce mouse input amount to be equivalent to stick movement
				i *= 0.01f;
#if UNITY_WEBGL
					// Mouse tends to be even more sensitive in WebGL due to mouse acceleration, so reduce it even more
					i *= WebglLookSensitivityMultiplier;
#endif

				return i;
			} else {
				return 0f;
			}
		}
	}
}