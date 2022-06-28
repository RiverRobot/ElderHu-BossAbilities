using Modding;
using System;
using System.Collections.Generic;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using UnityEngine;
using Satchel;
using Satchel.Futils;
using System.Collections;
using GlobalEnums;

namespace ElderHuBossAbilities
{
    public class ElderHuBossAbilities : Mod
    {
        private static ElderHuBossAbilities? _instance;

        internal static ElderHuBossAbilities Instance
        {
            get
            {
                if (_instance == null)
                {
                    throw new InvalidOperationException($"{nameof(ElderHuBossAbilities)} was never initialized");
                }
                return _instance;
            }
        }

        public override string GetVersion() => GetType().Assembly.GetName().Version.ToString();

        public ElderHuBossAbilities() : base()
        {
            _instance = this;
        }

        public override List<(string, string)> GetPreloadNames()
        {
            return new List<(string, string)>()
            {
                ("GG_Ghost_Hu", "Warrior/Ghost Warrior Hu"),
                ("GG_Ghost_Hu", "Ring Holder/1")
            };
        }
        public static GameObject Rings;
        public static GameObject Hu;
        public static AudioSource audioSource;

        public override void Initialize(Dictionary<string, Dictionary<string, GameObject>> preloadedObjects)
        {
            Log("Initializing");

            Hu = preloadedObjects["GG_Ghost_Hu"]["Warrior/Ghost Warrior Hu"];
            Rings = preloadedObjects["GG_Ghost_Hu"]["Ring Holder/1"];

            var go = new GameObject("AudioSource");
            audioSource = go.AddComponent<AudioSource>();
            audioSource.pitch = .75f;
            audioSource.volume = .3f;
            UnityEngine.Object.DontDestroyOnLoad(audioSource);

            On.PlayMakerFSM.Awake += FSMAwake;
            Log("Initialized");
        }

        private void FSMAwake(On.PlayMakerFSM.orig_Awake orig, PlayMakerFSM self)
        {
            if (self.FsmName == "Dream Nail")
            {
                FsmState castRings = self.GetState("Set Antic");
                castRings.InsertCustomAction("Set Antic", () => CastHuRings(), 10);
                castRings.GetAction<CustomFsmAction>(10).Enabled = true;
            }

            orig(self);
        }

        public class MonoBehaviour1 : MonoBehaviour
        {
            public GameObject AttachedDetector;
            public void Start()
            {
                audioSource.pitch = 1f;
                audioSource.volume = .1f;
                var audioClip = Hu.LocateMyFSM("Attacking").GetState("Ring Antic").GetAction<AudioPlayerOneShotSingle>(1).audioClip.Value as AudioClip;
                audioSource.PlayOneShot(audioClip);
                ;
            }

            public void OnTriggerEnter2D(Collider2D collision)
            {
                Rigidbody2D rb2d;

                if (collision.gameObject.layer == (int)PhysLayers.TERRAIN)
                {

                    rb2d = gameObject.GetAddComponent<Rigidbody2D>();
                    rb2d.velocity = Vector3.zero;

                    gameObject.Find("Box Big").gameObject.SetActive(false);
                    gameObject.Find("Box Impact").gameObject.SetActive(true);
                    GameManager.instance.StartCoroutine(DestroyRing());

                }
                
            }
            public IEnumerator DestroyRing()
            {
                audioSource.pitch = .85f;
                audioSource.volume = .2f;
                var audioClip = Hu.LocateMyFSM("Attacking").GetState("Attack").GetAction<AudioPlayerOneShotSingle>(2).audioClip.Value as AudioClip;
                audioSource.PlayOneShot(audioClip);
                var clipland = Rings.LocateMyFSM("Control").GetState("Land").GetAction<Tk2dPlayAnimationWithEvents>(1).clipName.Value;
                gameObject.GetComponent<tk2dSpriteAnimator>().Play(clipland);
                yield return new WaitUntil(() => gameObject.GetComponent<tk2dSpriteAnimator>().IsPlaying(clipland) == false);
                var clipland2 = Rings.LocateMyFSM("Control").GetState("Land 2").GetAction<Tk2dPlayAnimationWithEvents>(0).clipName.Value;
                gameObject.GetComponent<tk2dSpriteAnimator>().Play(clipland2);
                yield return new WaitUntil(() => gameObject.GetComponent<tk2dSpriteAnimator>().IsPlaying(clipland2) == false);
                GameObject.Destroy(gameObject);
                GameObject.Destroy(AttachedDetector);
            }
        }
        public class DetectorMonobehaviour : MonoBehaviour
        {
            Rigidbody2D rb2d;
            public GameObject AttachedRing;
            public void Start()
            {
                
            }
            public void OnTriggerEnter2D(Collider2D collision)
            {
                if (collision.gameObject.layer == (int)PhysLayers.ENEMIES)
                {
                    AttachedRing.Find("Box Up").gameObject.SetActive(false);
                    var clipdown = Rings.LocateMyFSM("Control").GetState("Down").GetAction<Tk2dPlayAnimation>(0).clipName.Value;
                    AttachedRing.GetComponent<tk2dSpriteAnimator>().Play(clipdown);
                    AttachedRing.GetComponent<MeshRenderer>().enabled = true;
                    AttachedRing.Find("Box Big").gameObject.SetActive(true);
                    rb2d = AttachedRing.GetAddComponent<Rigidbody2D>();
                    rb2d.velocity = new Vector2(0, -60);
                }
            }
        }
        public List<GameObject> previousrings = new List<GameObject>();
        private void CastHuRings()
        {
           

            foreach (GameObject eldenring in previousrings)
            {
                if (eldenring != null)
                {
                    GameObject.Destroy(eldenring);
                    Log("Destroyed old ring");
                }
            }
            PlayMakerFSM dnail = HeroController.instance.gameObject.LocateMyFSM("Dream Nail");
            dnail.RemoveTransition("Set Antic", "FINISHED");
            dnail.AddTransition("Set Antic", "FINISHED", "Set");
            dnail.RemoveTransition("Set", "FINISHED");
            dnail.AddTransition("Set", "FINISHED", "Set Recover");
            /*
            Fix transitions
            dnail.RemoveTransition("Set Antic", "FINISHED");
            dnail.AddTransition("Set Antic", "FINISHED", "Can Set?");
            dnail.RemoveTransition("Set","FINISHED")
            dnail.AddTransition("Set", "FINISHED", "Spawn Gate")
            */
            float[] exclude = new float[6];
            for (int x = 0; x < 6; x++)
            {
                exclude[x] = UnityEngine.Random.Range(-6, 6);
                exclude[x] = exclude[x] *= 2.5f;
            }



            for (float i = -15f; i <= 15; i += 2.5f)
            {
                bool stoploop = false;
                foreach (var Val in exclude)
                {
                    if (Val == i)
                    {
                        stoploop = true;
                    }

                }
                if (stoploop)
                {
                    continue;
                }

                var ring = GameObject.Instantiate(Rings);
                previousrings.Add(ring);
                GameObject.Destroy(ring.LocateMyFSM("Control"));
                ring.SetActive(true);
                ring.layer = (int)PhysLayers.HERO_ATTACK;
                ring.AddComponent<NonBouncer>();
                GameObject.Destroy(ring.GetComponent<DamageHero>());
                GameObject.Destroy(ring.GetComponentInChildren<DamageHero>());
                GameObject.Destroy(ring.Find("Box Small").GetComponent<DamageHero>());
                GameObject.Destroy(ring.Find("Box Big").GetComponent<DamageHero>());
                GameObject.Destroy(ring.Find("Box Up").GetComponent<DamageHero>());
                GameObject.Destroy(ring.Find("Box Impact").GetComponent<DamageHero>());
                AddDamageEnemy(ring);
                ring.transform.position = HeroController.instance.transform.position - new Vector3(i, -5, 0);
                ring.AddComponent<MonoBehaviour1>();
                GameObject detector = new();
                detector.name = "Enemy Detector";
                detector.gameObject.SetActive(false);
                detector.AddComponent<BoxCollider2D>();
                detector.AddComponent<DetectorMonobehaviour>();
                var col = detector.GetComponent<BoxCollider2D>();
                ring.GetComponent<MonoBehaviour1>().AttachedDetector = detector;
                detector.GetComponent<DetectorMonobehaviour>().AttachedRing = ring;
                col.isTrigger = true;
                col.size = new Vector2(1.85f, 12);
                col.gameObject.layer = (int)PhysLayers.HERO_ATTACK;
                detector.transform.position = ring.transform.position - new Vector3(-0.125f, 6.25f, 0);


                GameManager.instance.StartCoroutine(RingCoroutine(ring, detector));



            }

        }


        private IEnumerator RingCoroutine(GameObject ring, GameObject detector)
        {
            Rigidbody2D rb2d = ring.GetComponent<Rigidbody2D>();
            var clipantic = Rings.LocateMyFSM("Control").GetState("Antic").GetAction<Tk2dPlayAnimationWithEvents>(0).clipName.Value;
            ring.GetComponent<tk2dSpriteAnimator>().Play(clipantic);
            ring.Find("Box Small").gameObject.SetActive(true);
            yield return new WaitUntil(() => ring.GetComponent<tk2dSpriteAnimator>().IsPlaying(clipantic) == false);
            ring.Find("Box Up").gameObject.SetActive(true);
            ring.Find("Box Small").gameObject.SetActive(false);
            rb2d.velocity = new Vector3(0, 2, 0);
            var clipantic2 = Rings.LocateMyFSM("Control").GetState("Antic 2").GetAction<Tk2dPlayAnimationWithEvents>(2).clipName.Value;
            ring.GetComponent<tk2dSpriteAnimator>().Play(clipantic2);
            yield return new WaitForSeconds(0.25f);
            rb2d.velocity = Vector3.zero;
            detector.gameObject.SetActive(true);
        }


        public static DamageEnemies AddDamageEnemy(GameObject go)
        {
            var dmg = go.GetAddComponent<DamageEnemies>();
            dmg.attackType = AttackTypes.Spell;
            dmg.circleDirection = false;
            dmg.damageDealt = 15;
            dmg.direction = 90 * 3;
            dmg.ignoreInvuln = false;
            dmg.magnitudeMult = 1f;
            dmg.moveDirection = false;
            dmg.specialType = 0;
            return dmg;
        }
    }
}
