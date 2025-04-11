using System.IO;
using TeamHeroCoderLibrary;

namespace PlayerCoder
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Connecting...");
            GameClientConnectionManager connectionManager;
            connectionManager = new GameClientConnectionManager();
            connectionManager.SetExchangePath(MyAI.FolderExchangePath);
            connectionManager.onHeroHasInitiative = MyAI.ProcessAI;
            connectionManager.StartListeningToGameClientForHeroPlayRequests();
        }
    }

    public static class MyAI
    {
        public static string FolderExchangePath = "C:/Users/davis/AppData/LocalLow/Wind Jester Games/Team Hero Coder";
        const int RESURRECTION_COST = 25;
        const int QUICK_HIT_COST = 15;
        const int CURE_SERIOUS_COST = 20;
        const int DEBRAVE_COST = 10;
        const int DEFAITH_COST = 10;
        const int FLURRY_OF_BLOWS_COST = 15;
        const int AUTO_LIFE_COST = 25;
        const int POISON_NOVA_COST = 15;
        const int MAGIC_MISSILE_COST = 10;
        const int METEOR_COST = 60;
        const int QUICK_CLEANSE_COST = 10;
        const int FAITH_COST = 15;


        static public void ProcessAI()
        {
            Console.WriteLine("Processing AI!");
            Hero activeHero = null;
            bool hasPerformedAction = false;
            bool isSilenced = false;
            bool hasDebrave = false;
            float useEtherAmount = 0.3f;
            bool hasEther = false;
            bool hasAutoLife = false;
            bool hasPoisonEffect = false;

            #region Code
            #region Monk
            if (TeamHeroCoder.BattleState.heroWithInitiative.jobClass == HeroJobClass.Monk)
            {

                activeHero = TeamHeroCoder.BattleState.heroWithInitiative;

                Console.WriteLine("this is a Monk");
                Hero target = null;

                if (HasStatus(activeHero, StatusEffect.Silence))
                {
                    Console.WriteLine("We are silenced Shhhh");
                    isSilenced = true;
                }


                if (!hasPerformedAction)
                {
                    Console.WriteLine("Checking for Brave!");
                    foreach (Hero foe in TeamHeroCoder.BattleState.foeHeroes)
                    {
                        hasDebrave = false;
                        if (HasStatus(foe, StatusEffect.Debrave))
                        {
                            Console.WriteLine("Already made them Cowards!");
                            hasDebrave = true;
                        }

                        if (HasStatus(foe, StatusEffect.Brave) 
                            && activeHero.mana >= DEBRAVE_COST 
                            && !isSilenced 
                            && !hasDebrave)
                        {
                            if (!hasPerformedAction)
                            {
                                Console.WriteLine("Target is Brave. Make them a coward!");
                                hasPerformedAction = AttemptToPerformAction(hasPerformedAction, Ability.Debrave, foe);
                            }
                        }
                    }
                }
                if (!hasPerformedAction)
                {
                    Console.WriteLine("Checking if they have Faith!");
                    foreach (Hero foe in TeamHeroCoder.BattleState.foeHeroes)
                    {
                        if (HasStatus(foe, StatusEffect.Faith) 
                            && activeHero.mana >= DEFAITH_COST 
                            && !isSilenced)
                        {

                            if (!hasPerformedAction)
                            {
                                Console.WriteLine("Target has Faith. Casting Blasphemy");
                                hasPerformedAction = AttemptToPerformAction(hasPerformedAction, Ability.Defaith, foe);
                            }
                        }

                    }
                }
                if (!hasPerformedAction)
                {
                    Console.WriteLine("Checking if anyone is Poisoned");
                    foreach (Hero foe in TeamHeroCoder.BattleState.foeHeroes)
                    {
                        if (HasStatus(foe, StatusEffect.Poison) 
                            && activeHero.mana >= FLURRY_OF_BLOWS_COST 
                            && !isSilenced)
                        {

                            if (!hasPerformedAction)
                            {
                                Console.WriteLine("Target is Poisoned. Hit them where it hurts!");
                                hasPerformedAction = AttemptToPerformAction(hasPerformedAction, Ability.FlurryOfBlows, foe);
                            }
                        }

                    }
                }


                foreach (Hero hero in TeamHeroCoder.BattleState.foeHeroes)
                {

                    if (hero.health > 0)
                    {
                        if (target == null)
                            target = hero;
                        else if (hero.health < target.health)
                            target = hero;
                    }
                }
                if (!hasPerformedAction)
                {
                    Console.WriteLine("No Statuses on Enemy team. Punch lowest health enemy in the face!");
                    hasPerformedAction = AttemptToPerformAction(hasPerformedAction, Ability.Attack, target);
                }
            }
            #endregion Monk
            #region Cleric
            else if (TeamHeroCoder.BattleState.heroWithInitiative.jobClass == HeroJobClass.Cleric)
            {
                activeHero = TeamHeroCoder.BattleState.heroWithInitiative;
                int statusEffectCount = 0;

                //The character with initiative is a cleric, do something here...

                Console.WriteLine("this is a cleric");
                Hero target = null;

                if (HasStatus(activeHero, StatusEffect.Silence))
                {
                    Console.WriteLine("we are silenced Shhhh");
                    isSilenced = true;
                }

                if (!hasPerformedAction)
                {
                    Console.WriteLine("Does anyone need healing?");
                    foreach (Hero ally in TeamHeroCoder.BattleState.allyHeroes)
                    {
                        if ((float)ally.health / (float)ally.maxHealth <= 0.3f)
                        {
                            Console.WriteLine("We found a wounded ally");
                            if (activeHero.mana >= CURE_SERIOUS_COST + RESURRECTION_COST 
                                && ally.health > 0 
                                && !isSilenced)
                            {
                                Console.WriteLine("We have the Mana for Cure Serious with enough left over for Resurrection. Casting it.");
                                hasPerformedAction = AttemptToPerformAction(hasPerformedAction, Ability.CureSerious, ally);
                                break;

                            }
                            else
                            {
                                Console.WriteLine("We dont have enough mana to make sure we can cast resurrection, skipping the heal.");
                            }
                        }
                    }
                }
                Console.WriteLine("Do we have any Ethers?");
                hasEther = HasItem(Item.Ether);
                if (hasEther) Console.WriteLine("We still have Ethers");
                


                if (!hasPerformedAction)
                {
                    foreach (Hero ally in TeamHeroCoder.BattleState.allyHeroes)
                    {
                        if (ManaPercent(ally) <= useEtherAmount 
                            && hasEther 
                            && ally.health > 0)
                        {
                            Console.WriteLine("An ally is below 30% Mana. Force them to drink!");
                            hasPerformedAction = AttemptToPerformAction(hasPerformedAction, Ability.Ether, ally);

                        }
                    }
                }
                if (!hasPerformedAction)
                {
                    Console.WriteLine("Does the Wizard or I need Faith?");
                    foreach (Hero ally in TeamHeroCoder.BattleState.allyHeroes)
                    {
                        if (ally.jobClass == HeroJobClass.Wizard)
                        {
                            if (!HasStatus(ally, StatusEffect.Faith) 
                                && activeHero.mana >= FAITH_COST 
                                && !isSilenced 
                                && ally.health > 0)
                            {
                                    Console.WriteLine("Target is not Faithed. Throw the Bible at them!");
                                    hasPerformedAction = AttemptToPerformAction(hasPerformedAction, Ability.Faith, ally);
                            }
                        }
                    }
                }
                if (!hasPerformedAction)
                {
                    Console.WriteLine("Checking for Negative Status effects");
                    foreach (Hero ally in TeamHeroCoder.BattleState.allyHeroes)
                    {
                        if (GetNegativeStatusEffectCount(ally) >= 1)
                        {
                            if (activeHero.mana >= QUICK_CLEANSE_COST 
                                && !isSilenced
                                && ally.health > 0)
                            {
                                    Console.WriteLine("Target has Several Negative Ailments. Cleanse them!");
                                    hasPerformedAction = AttemptToPerformAction(hasPerformedAction, Ability.QuickCleanse, ally);
                            }
                            else
                            {
                              Console.WriteLine("We don't have enough mana to cast QuickCleanse, or we are silenced.");
                            }
                        }
                    }
                }
                if (!hasPerformedAction)
                {
                    Console.WriteLine("is Anyone dead?");
                    foreach (Hero ally in TeamHeroCoder.BattleState.allyHeroes)
                    {
                        if (ally.health <= 0)
                        {
                            Console.WriteLine("We found a dead ally");
                            if (activeHero.mana >= RESURRECTION_COST && !isSilenced)
                            {
                                Console.WriteLine("We have the Mana to revive someone! REVIVE THEM!");
                                hasPerformedAction = AttemptToPerformAction(hasPerformedAction, Ability.Resurrection, ally);
                            }
                            else
                            {
                                Console.WriteLine("We dont have enough mana. skipping");
                            }
                        }
                    }
                    if (!hasPerformedAction)
                    {
                        Console.WriteLine("Checking for petrifying or petrified");
                        foreach (Hero ally in TeamHeroCoder.BattleState.allyHeroes)
                        {
                            
                            if (HasStatus(ally, StatusEffect.Petrifying) || HasStatus(ally, StatusEffect.Petrified))
                            {
                                Console.WriteLine("We found petrified or petrifying. They are Dirty! clean them!");
                                if (activeHero.mana >= QUICK_CLEANSE_COST 
                                    && !isSilenced 
                                    && ally.health > 0)
                                {
                                    hasPerformedAction = AttemptToPerformAction(hasPerformedAction, Ability.QuickCleanse, ally);
                                }
                                else
                                {
                                    Console.WriteLine("We don't have enough mana to cast QuickCleanse, or we are silenced.");
                                }
                            }
                        } 
                    }
                    if (!hasPerformedAction)
                    {
                        Console.WriteLine("No Healing or Revive needed. AUTO LIFE EVERYTHING!");
                        foreach (Hero ally in TeamHeroCoder.BattleState.allyHeroes)
                        {
                            hasAutoLife = false;
                            if (HasStatus(ally, StatusEffect.AutoLife))
                            {
                                Console.WriteLine("We found an ally with Auto Life, skipping this hero");
                                hasAutoLife = true;
                                continue;
                            }

                            if (activeHero.mana >= AUTO_LIFE_COST 
                                && !isSilenced 
                                && !hasAutoLife 
                                && ally.health > 0)
                            {
                                    Console.WriteLine("Found someone without Autolife and We have the Mana for it!");
                                    hasPerformedAction = AttemptToPerformAction(hasPerformedAction, Ability.AutoLife, ally);
                            }
                            else
                            {
                                Console.WriteLine("We don't have enough mana to cast Auto Life, or we are silenced.");
                            }
                        }
                    }

                    foreach (Hero hero in TeamHeroCoder.BattleState.foeHeroes)
                    {
                        if (hero.health > 0)
                        {
                            if (target == null)
                                target = hero;
                            else if (hero.health < target.health)
                                target = hero;
                        }
                    }

                    if (!hasPerformedAction)
                    {
                        Console.WriteLine("No Healing or Revive needed. Everyone has Auto life.... ummmmm Hit the weakest person?");
                        hasPerformedAction = AttemptToPerformAction(hasPerformedAction, Ability.Attack, target);
                    }
                }
            }
            #endregion Cleric
            #region Wizard
            else if (TeamHeroCoder.BattleState.heroWithInitiative.jobClass == HeroJobClass.Wizard)
            {
                //The character with initiative is a wizard, do something here...
                activeHero = TeamHeroCoder.BattleState.heroWithInitiative;

                Console.WriteLine("this is a wizard");
                Hero target = null;


                if (HasStatus(activeHero, StatusEffect.Silence))
                {
                    Console.WriteLine("we are silenced Shhhh");
                    isSilenced = true;
                }


                Console.WriteLine("Do we have any Ethers?");
                hasEther = HasItem(Item.Ether);
                if (hasEther) Console.WriteLine("We still have Ethers");


                if (!hasPerformedAction)
                {
                    foreach (Hero ally in TeamHeroCoder.BattleState.allyHeroes)
                    {
                        if (ManaPercent(ally) <= useEtherAmount && hasEther && ally.health > 0)
                        {
                            Console.WriteLine("An ally is below 30% Mana. Using an Ether");
                            hasPerformedAction = AttemptToPerformAction(hasPerformedAction, Ability.Ether, ally);

                        }
                    }
                }
                if (!hasPerformedAction && activeHero.mana >= POISON_NOVA_COST)
                {
                    Console.WriteLine("Wizard's total MP is greater than the cost of Poison Nova!");
                    Console.WriteLine("Checking for Posioned enemies");
                    foreach (Hero foe in TeamHeroCoder.BattleState.foeHeroes)
                    {

                        if (HasStatus(foe, StatusEffect.Poison))
                        {
                            Console.WriteLine("We found a poisoned enemy");
                            hasPoisonEffect = true;
                            break;
                        }

                        if (!hasPoisonEffect 
                            && foe.health > 0 
                            && !isSilenced)
                        {
                                Console.WriteLine("Found someone who isn't poisoned and they are Alive");
                                Console.WriteLine("We have the Mana for Poison Nova!");
                                hasPerformedAction = AttemptToPerformAction(hasPerformedAction, Ability.PoisonNova, foe);
                        }
                        else
                        {
                            Console.WriteLine("Not enough mana for Poison Nova!");
                        }
                    }
                }
                if (!hasPerformedAction)
                {
                    List<Hero> standingFoes = new List<Hero>();

                    foreach (Hero foe in TeamHeroCoder.BattleState.foeHeroes)
                    {
                        if (foe.health > 0)
                        {
                            standingFoes.Add(foe);
                        }
                    }

                    // Use Meteor if 2 or more foes are still standing
                    if (standingFoes.Count >= 1 
                        && activeHero.mana >= METEOR_COST 
                        && !isSilenced)
                    {
                        Console.WriteLine("More than 2 foes standing, Make them regret it! METEOR!");
                        hasPerformedAction = AttemptToPerformAction(hasPerformedAction, Ability.Meteor);
                        return;
                    }

                    foreach (Hero hero in TeamHeroCoder.BattleState.foeHeroes)
                    {
                        if (hero.health > 0)
                        {
                            if (target == null)
                                target = hero;
                            else if (hero.health < target.health)
                                target = hero;
                        }
                    }
                }

                if (!hasPerformedAction 
                    && activeHero.mana >= MAGIC_MISSILE_COST 
                    && !isSilenced)
                {
                    Console.WriteLine("Nothing to do! Targeting enemy with lowest HP");
                    Console.WriteLine("Magic Missile!");
                    hasPerformedAction = AttemptToPerformAction(hasPerformedAction, Ability.MagicMissile, target);
                }
                if (!hasPerformedAction)
                {
                    Console.WriteLine("Nothing to do! Targeting enemy with lowest HP");
                    Console.WriteLine("out of Mana! hit them with your stick!");
                    hasPerformedAction = AttemptToPerformAction(hasPerformedAction, Ability.Attack, target);
                }
            }
            #endregion Wizard
            #region samplecode
            foreach (InventoryItem ii in TeamHeroCoder.BattleState.allyInventory)
            {
                //How we look THROUGH our inventory
                if (ii.item == Item.Potion)
                {
                    //We found a potion
                }
            }


            //Searching for a poisoned hero 
            foreach (Hero hero in TeamHeroCoder.BattleState.allyHeroes)
            {
                foreach (StatusEffectAndDuration se in hero.statusEffectsAndDurations)
                {
                    if (se.statusEffect == StatusEffect.Poison)
                    {
                        //We have found a character that is poisoned, do something here...
                    }
                }
            }
            #endregion samplecode
            #endregion

        }

        #region Functions
        static public bool AttemptToPerformAction(bool hasPerformedAction, Ability ability, Hero? target = null)
        {
            if (!hasPerformedAction)
            {
                TeamHeroCoder.PerformHeroAbility(ability, target);
                return true;
            }

            return hasPerformedAction;
        }

        static public bool HasStatus(Hero hero, StatusEffect effect)
        {
            foreach (StatusEffectAndDuration se in hero.statusEffectsAndDurations)
            {
                if (se.statusEffect == effect) return true;
            }

            return false;
        }

        static public bool HasItem(Item item)
        {
            foreach (InventoryItem ii in TeamHeroCoder.BattleState.allyInventory)
            {
                if (ii.item == item && ii.count > 0) return true;
            }
            return false;
        }

        static public float ManaPercent(Hero hero)
        {
            float manaPercent = 0.0f;

            manaPercent = (float)hero.mana / (float)hero.maxMana;

            return manaPercent;
        }

        static public int GetNegativeStatusEffectCount(Hero hero)
        {
            int statusEffectCount = 0;
            foreach (StatusEffectAndDuration se in hero.statusEffectsAndDurations)
            {
                if (se.statusEffect == StatusEffect.Defaith) statusEffectCount++;
                if (se.statusEffect == StatusEffect.Debrave) statusEffectCount++;
                if (se.statusEffect == StatusEffect.Doom) statusEffectCount++;
                if (se.statusEffect == StatusEffect.Petrifying) statusEffectCount++;
                if (se.statusEffect == StatusEffect.Petrified) statusEffectCount++;
                if (se.statusEffect == StatusEffect.Silence) statusEffectCount++;
                if (se.statusEffect == StatusEffect.Poison) statusEffectCount++;
                if (se.statusEffect == StatusEffect.Slow) statusEffectCount++;
            }
            return statusEffectCount;
        }
        #endregion Functions
    }
}