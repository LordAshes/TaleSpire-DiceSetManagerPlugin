# Dice Set Manager Plugin

This unofficial TaleSpire plugin for creating, rolling and clearing dice sets. This dice set intercepts the core
TS dice functionality and instead of visualizing the results allows the user to subscribe to the results of the
rolls so that they can be used and/or visualized using a custom manner. Dice created with this plugin can be rolled
both manually (by the player) like core TS dice or automatically from code (such as a ruleset implementation).

## Change Log

1.0.1: Fixed documentation formatting

1.0.0: Initial release

## Install

Use R2ModMan or similar installer to install this plugin.

## Usage

This is a dependency plugin which does not do anything on its own. Instead, it is used by other plugins to take care
of the dice set functionality. The plugin has both basic and advanced features.

This plugin is ideal when the results of the rolls are not to be automatically displayed such as when implementing
the game mechanics for a ruleset. If you want to retain the original core TS dice result display then consider
using Hollo's DiceCallbackPlugin instead.

### Basic Features

#### CreateDiceSet(string dicesetName, string formula)

This method creates dice set (consisting of one more dice and one or more modifiers) with the given name.
The dice will be created in the TS dice tray, automatically moved to the board, and registed so that other
methods can act on the dice set using the provided name. Example:

DiceSetManager.CreateDiceSet("Longbow Damage", "1D8+3+2D6")

#### ThrowDiceSet(string dicesetName, float vertical)

This method takes all the dice associated with the indicated dice set, gathers them, lifts them, and rolls them.
The second parameter, vertical, is optional. It determines how far the dice are lifted before they are rolled.
Examples:

DiceSetManager.ThrowDiceSet("Longbow Damage")
DiceSetManager.ThrowDiceSet("Longbow Damage", 7.0f)

#### ClearDiceSet(string dicesetName)

This method unregisters the indicated dice set and removes all dice associated with that dice set. Use this method,
instead of the core TS methods, to remove dice so that the dice set is properly unregistered. Example:

DiceSetManager.ClearDiceSet("Longbow Damage")

#### ClearAllDiceSets()

This method unregisters and all the registered dice sets and removes all the dice associated with the dice sets.
Use this functionality to remove dice sets, instead of using the core TS methods, in order to ensure that the
dice sets are properly unregistered. Example:

DiceSetManager.ClearAllDiceSets()

#### Subscribe(SubscriptionEvent action, string identity, Action<string> callback)

This method provides a way to subscribe to dice set events. The most common dice set event to subscribe to is the
dice roll result event but it is also possible to subscribe to the dice set added and dice set cleared events.
The SubscriptionEvent indicates which event is to be subscribed to. To subscrive to multiple events, either call
the Subscrive method multiple times or separate the different subscription events by a pipe (or operator). The
identity is any unique string that can be used in the Unsubscribe method to remove subscriptions for the indicated
identity. This allows different plugins to subscribe to events without affecting each other. The callback parameter
indicates the function that should be called when the event occurs. The callback function takes single string as
a parameter which passes to it the event arguments as a JSON string. The event arguments either identifies that
dice set that was created, provides the results of a dice set roll (both total and details) or provides the name
and roll id of the dice set that was cleared. Examples:
  
Subscribe(SubscriptionEvent.diceResult, "DiceSetManagerParentPlugin", (s)=>{ Debug.Log(s); });
Subscribe(SubscriptionEvent.diceAdd | SubscriptionEvent.diceResult, "DiceSetManagerParentPlugin", (s)=>{ Debug.Log(s); });

### Advanced Features

#### diceSets

DiceSetManager.diceSets is a dictionary which is indexed by the dice set name and contains a DiceSet. A DiceSet contains
the RollId (which is typically used internally) and Dice whcih is a List<Die> that identifies each of the Die objects in
the dice set. These can be used to perform operations on the individual dice of a set (like re-roll a single die). The
Die objects provide access to the actual Die game object in case the actual game object (such as the mesh) needs to be
modified.
