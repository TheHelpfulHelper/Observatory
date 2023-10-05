# Observatory
 
Observatory is an open-source reactive state-management library built for VRChat using U#.
It automatically handles state synchronization while enabling a reactive design pattern.

## Requirements

Observatory requires [VRRefAssist](https://github.com/LiveDimensions/VRRefAssist) to be installed.

## Getting Started

**For an implementation example, take a look at the example scene in the "Prefabricated" folder!**

### Installation
1. Ensure you have installed VRRefAssist
2. Download the current release unitypackage
3. Import the unitypackage into your Unity project

### What is State?

"State" is the source-of-truth that should contain everything you need to know to recreate the current visual state of your world. In essence it is the bare minimum information that everything else derives from.

For example:

In a chess game your "State" would consist of the positions of every piece, which players are participating in the match and who's turn it is. 

In a FPS you would want to keep track of how much health every player has, which weapon they have currently equipped and which team they are on. (Normally you would also keep track of where every player is, but VRChat does this for us)

State can be split up into smaller "Sub-States", so together all "Sub-States" form your worlds "State"

Every "State Host" holds such a "Sub-State"

This is important for both bandwidth considerations and performance as every "State Host" object will serialize its entire "Sub-State" and on deserialization will perform the nessecary checks for the observers to react.

# **IMPORTANT !!!**

**Values that often change together SHOULD be kept together on one State Host and values that dont change together often SHOULD NOT be kept together on one State Host.**

Everything happening inside of the Observatory SHOULD be pure and functional and SHOULD NOT use any extra variables or cause any side-effects.

### What is an Observer?

An observer observes state. Specifically it observes a specific field in the "State". In Observatory this is based on a path, so an Observer could observe "chess.whitePiecese.king.position" and when the position of the white king changes it will be notified. An observer can observe multiple state fields at the same time. It can also observe other observers. The Observatory ensures that all observers are notified in the correct order based on their position in the dependency-tree.

### What is an Effect?

An effect is an observer that being notified of a change does something "useful". Its anything that changes the environment outside of the Observatory, Unity calls, VRChat Player API calls, etc. Anything that doesnt happen inside your own code in the Observatory.
