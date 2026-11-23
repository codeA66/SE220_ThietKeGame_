using System.Collections;
using System.Linq;
using DeadCells.Combat;
using DeadCells.Enemies;
using DeadCells.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace DeadCells.NhaGiamGiengChuong.Tests.Play
{
    public sealed class EncounterPlayTests : InputTestFixture
    {
        private Keyboard _keyboard;

        public override void Setup()
        {
            base.Setup();
            _keyboard = InputSystem.AddDevice<Keyboard>();
        }

        public override void TearDown()
        {
            Time.timeScale = 1f;
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator P02_DormantEnemiesShieldBlockAndArcherArrowAcrossRoomEOneWayPlatform()
        {
            yield return PlayModeTestHelper.LoadLevelScene();
            var run = UnityEngine.Object.FindAnyObjectByType<LevelRunController>();
            var player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
            var projSpawner = UnityEngine.Object.FindAnyObjectByType<ProjectileSpawner>();
            yield return PlayModeTestHelper.WaitForPlaying(run);

            var enemies = UnityEngine.Object.FindObjectsByType<EnemyController>();
            var zones = UnityEngine.Object.FindObjectsByType<EncounterZone>();
            var zombieB = enemies.Single(e => e.name == "Enemy_Zombie_B");
            var ratG = enemies.Single(e => e.name == "Enemy_Rat_G");
            var shieldJ = enemies.Single(e => e.name == "Enemy_Shield_J");
            var zombieE = enemies.Single(e => e.name == "Enemy_Zombie_E");
            var archerE = enemies.Single(e => e.name == "Enemy_Archer_E");

            var zoneB1 = zones.Single(z => z.ZoneId == "B1");
            var zoneG1 = zones.Single(z => z.ZoneId == "G1");
            var zoneJ1 = zones.Single(z => z.ZoneId == "J1");
            var zoneE1 = zones.Single(z => z.ZoneId == "E1");

            // 1. All 5 enemies visible at spawn, dormant, stationary, and damage-immune before activation
            var initialPositions = enemies.ToDictionary(e => e, e => e.transform.position);
            foreach (var e in enemies)
            {
                Assert.IsTrue(e.gameObject.activeInHierarchy, $"[P02/Dormant] {e.name} must be active.");
                var sr = e.GetComponentInChildren<SpriteRenderer>();
                Assert.IsNotNull(sr);
                Assert.IsTrue(sr.enabled, $"[P02/Dormant] {e.name} renderer must be visible.");
                Assert.IsFalse(e.enabled, $"[P02/Dormant] {e.name} controller must be dormant.");
            }

            // Wait past idle tick & verify direct TakeDamage is vetoed by dormant filter
            yield return new WaitForSeconds(0.4f);
            foreach (var e in enemies)
            {
                Assert.AreEqual(initialPositions[e], e.transform.position, $"[P02/Dormant] {e.name} must not move.");
                var h = e.GetComponent<Health>();
                int before = h.Current;
                Assert.IsFalse(h.TakeDamage(new DamageInfo(25, 1, 0f, 0f, false, player.gameObject)),
                    $"[P02/Dormant] {e.name} must be immune while sleeping.");
                Assert.AreEqual(before, h.Current);
            }

            // 2. Walk across Room G upper floor y=8 -> Rat G stays asleep
            PlayModeTestHelper.PositionPlayerAtFeet(player, new Vector2(68f, 8f));
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.IsFalse(zoneG1.IsActivated, "[P02/GUpper] Walking at y=8 must NOT wake Rat G.");
            Assert.IsFalse(ratG.enabled);

            // Drop into G lower floor y=0 -> Rat G wakes once
            PlayModeTestHelper.PositionPlayerAtFeet(player, new Vector2(68f, 0f));
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.IsTrue(zoneG1.IsActivated, "[P02/GLower] Entering G lower must wake Rat G.");
            Assert.IsTrue(ratG.enabled);

            // 3. Activate B1 -> Zombie B wakes
            PlayModeTestHelper.PositionPlayerAtFeet(player, new Vector2(21.5f, 0f));
            yield return new WaitForFixedUpdate();
            Assert.IsTrue(zoneB1.IsActivated);
            Assert.IsTrue(zombieB.enabled);

            // 4. Activate J1 -> Shield J wakes; front hit (Stun=0) blocked, rear hit deals damage
            PlayModeTestHelper.PositionPlayerAtFeet(player, new Vector2(3.5f, 16f));
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.IsTrue(zoneJ1.IsActivated);
            Assert.IsTrue(shieldJ.enabled);

            // Wait 1 frame for Shield Start() to run so _machine.Current is initialized
            yield return null;
            var shieldHealth = shieldJ.GetComponent<Health>();
            int shieldMax = shieldHealth.Current;
            // EnemyContext.Facing starts at +1 (facing right): hit from right pushes left (Direction = -1 = -Facing => blocked!)
            bool frontHit = shieldHealth.TakeDamage(new DamageInfo(20, direction: -1, knockback: 1f, stun: 0f, isCritical: false, source: player.gameObject));
            Assert.IsFalse(frontHit, "[P02/ShieldFront] Front hit with Stun=0 must be blocked by Shield.");
            Assert.AreEqual(shieldMax, shieldHealth.Current);

            // Hit from behind pushes right (Direction = +1 => NOT fromFront => penetrates!)
            bool rearHit = shieldHealth.TakeDamage(new DamageInfo(20, direction: 1, knockback: 1f, stun: 0f, isCritical: false, source: player.gameObject));
            Assert.IsTrue(rearHit, "[P02/ShieldRear] Rear hit must damage Shield.");
            Assert.Less(shieldHealth.Current, shieldMax);

            // 5. Activate E1 -> Zombie E & Archer E wake; verify Archer arrow passes across center OneWayPlatform [-4, 2] @ y=26
            PlayModeTestHelper.PositionPlayerAtFeet(player, new Vector2(-13.5f, 24f));
            yield return new WaitForFixedUpdate();
            yield return null;
            Assert.IsTrue(zoneE1.IsActivated);
            Assert.IsTrue(zombieE.enabled);
            Assert.IsTrue(archerE.enabled);

            // Place Player west of center OneWayPlatform at (-6, 25.0) so chest/hurtbox is at y=25.875 (exact height of OneWay_E_Combat)
            PlayModeTestHelper.PositionPlayerAtFeet(player, new Vector2(-6f, 25.0f));
            var playerBody = player.GetComponent<Rigidbody2D>();
            float savedGravity = playerBody.gravityScale;
            playerBody.gravityScale = 0f;
            playerBody.linearVelocity = Vector2.zero;

            var arrowPrefab = archerE.Data.Skills.Select(s => s.ProjectilePrefab).FirstOrDefault(p => p != null);
            Assert.IsNotNull(arrowPrefab, "[P02/ArcherArrow] ArcherData must reference Projectile_Arrow.");
            var arrowHit = projSpawner.Get(arrowPrefab);
            int hpBeforeArrow = player.Health.Current;

            // Launch from east of center platform (x=4, y=25.875) leftward across [-4, 2] @ y=25.875 toward Player at x=-6
            arrowHit.LaunchStraight(new Vector2(4f, 25.875f), new Vector2(-18f, 0f), damage: 15, knockback: 1f, owner: archerE.gameObject);
            yield return PlayModeTestHelper.WaitUntil(
                () => player.Health.Current < hpBeforeArrow,
                2.5f,
                $"[P02/ArcherArrowHit] Arrow failed to cross Room E OneWayPlatform and hit Player. arrowLive={arrowHit.IsLive}, arrowPos={arrowHit.transform.position}");
            Assert.Less(player.Health.Current, hpBeforeArrow, "[P02/ArcherArrowHit] Player HP must drop after arrow crosses OneWayPlatform.");

            // Wait for Player i-frames to clear, then test a dodged arrow fired above Player
            yield return new WaitForSeconds(0.6f);
            int hpBeforeDodge = player.Health.Current;
            var arrowMiss = projSpawner.Get(arrowPrefab);
            arrowMiss.LaunchStraight(new Vector2(4f, 28.5f), new Vector2(-18f, 0f), damage: 15, knockback: 1f, owner: archerE.gameObject);
            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(hpBeforeDodge, player.Health.Current, "[P02/ArcherArrowDodge] Dodged arrow must not reduce Player HP.");

            playerBody.gravityScale = savedGravity;
        }

        [UnityTest]
        public IEnumerator P04_EncounterGateI4_UnlocksOnlyAfterBothRoomEEnemiesDieInEitherOrder()
        {
            // Order A: Zombie E first, retreat to K, verify still blocked at 1/2, then Archer E -> opens atomically
            yield return RunGateOrderVariant(killZombieFirst: true);

            // Order B: Archer E first, retreat to K, verify still blocked at 1/2, then Zombie E -> opens atomically
            yield return RunGateOrderVariant(killZombieFirst: false);
        }

        private IEnumerator RunGateOrderVariant(bool killZombieFirst)
        {
            string tag = killZombieFirst ? "ZombieThenArcher" : "ArcherThenZombie";
            yield return PlayModeTestHelper.LoadLevelScene();

            var run = UnityEngine.Object.FindAnyObjectByType<LevelRunController>();
            var player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
            var gate = UnityEngine.Object.FindAnyObjectByType<EncounterGate>();
            yield return PlayModeTestHelper.WaitForPlaying(run);

            var enemies = UnityEngine.Object.FindObjectsByType<EnemyController>();
            var zombieB = enemies.Single(e => e.name == "Enemy_Zombie_B");
            var zombieE = enemies.Single(e => e.name == "Enemy_Zombie_E");
            var archerE = enemies.Single(e => e.name == "Enemy_Archer_E");
            var barrier = gate.GateBarrierRoot;

            Assert.IsFalse(gate.IsOpen, $"[P04/{tag}/0of2] Gate starts closed.");
            Assert.IsTrue(barrier.activeInHierarchy, $"[P04/{tag}/0of2] Barrier active.");

            // Wake B1 and kill Zombie B -> outside E, must NOT affect I4
            PlayModeTestHelper.PositionPlayerAtFeet(player, new Vector2(21.5f, 0f));
            yield return new WaitForFixedUpdate();
            Assert.IsTrue(zombieB.GetComponent<Health>().TakeDamage(new DamageInfo(9999, 1, 0f, 0f, false, player.gameObject)));
            yield return null;
            Assert.AreEqual(0, gate.DefeatedCount, $"[P04/{tag}/OutsideE] Zombie B death must not count.");
            Assert.IsFalse(gate.IsOpen);

            // Enter E1 to wake Zombie E + Archer E
            PlayModeTestHelper.PositionPlayerAtFeet(player, new Vector2(-13.5f, 24f));
            yield return new WaitForFixedUpdate();
            yield return null;
            Assert.IsTrue(zombieE.enabled && archerE.enabled);

            // Kill first Room E enemy
            var first = killZombieFirst ? zombieE : archerE;
            var second = killZombieFirst ? archerE : zombieE;
            Assert.IsTrue(first.GetComponent<Health>().TakeDamage(new DamageInfo(9999, 1, 0f, 0f, false, player.gameObject)));
            yield return null;
            Assert.AreEqual(1, gate.DefeatedCount, $"[P04/{tag}/1of2]");
            Assert.IsFalse(gate.IsOpen);
            Assert.IsTrue(barrier.activeInHierarchy);

            // Retreat to K (-20, 24) and verify doorway K<->E stays open & progress remains 1/2
            PlayModeTestHelper.PositionPlayerAtFeet(player, new Vector2(-20f, 24f));
            yield return new WaitForFixedUpdate();
            yield return null;
            Assert.AreEqual(1, gate.DefeatedCount, $"[P04/{tag}/RetreatK] Progress preserved.");
            Assert.IsFalse(gate.IsOpen);

            // Try pushing right against closed barrier at x=20.5 -> Player cannot pass x=22
            PlayModeTestHelper.PositionPlayerAtFeet(player, new Vector2(20.5f, 24f));
            yield return null;
            Press(_keyboard.dKey);
            yield return new WaitForSeconds(0.45f);
            Release(_keyboard.dKey);
            Assert.Less(PlayModeTestHelper.GetPlayerFeet(player).x, 22f, $"[P04/{tag}/Blocked1of2] Closed gate must block passage.");

            // Kill second Room E enemy -> barrier visual & collider vanish simultaneously
            bool visualOffAtEvent = false;
            bool colliderOffAtEvent = false;
            gate.Opened += () =>
            {
                visualOffAtEvent = !barrier.GetComponent<SpriteRenderer>().gameObject.activeInHierarchy;
                colliderOffAtEvent = !barrier.GetComponent<BoxCollider2D>().gameObject.activeInHierarchy;
            };
            Assert.IsTrue(second.GetComponent<Health>().TakeDamage(new DamageInfo(9999, 1, 0f, 0f, false, player.gameObject)));
            yield return null;

            Assert.IsTrue(gate.IsOpen, $"[P04/{tag}/2of2] Gate must open.");
            Assert.IsTrue(visualOffAtEvent && colliderOffAtEvent, $"[P04/{tag}/AtomicOpen] Visual and collider must turn off together.");
            Assert.IsFalse(barrier.activeInHierarchy);

            // Walk right through opened x=22 into Room L (x > 23)
            Press(_keyboard.dKey);
            yield return PlayModeTestHelper.WaitUntil(
                () => PlayModeTestHelper.GetPlayerFeet(player).x > 23.5f,
                3f,
                $"[P04/{tag}/PassIntoL] Player could not walk through opened gate I4. feet={PlayModeTestHelper.GetPlayerFeet(player)}");
            Release(_keyboard.dKey);

            // Revisit lower floor & return -> gate stays open
            PlayModeTestHelper.PositionPlayerAtFeet(player, new Vector2(10f, 0f));
            yield return new WaitForFixedUpdate();
            Assert.IsTrue(gate.IsOpen, $"[P04/{tag}/StayOpen] Gate stays open across floor revisit.");
            Assert.IsFalse(barrier.activeInHierarchy);
        }
    }
}
