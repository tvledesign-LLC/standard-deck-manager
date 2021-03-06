﻿using System.Collections;
using System.Collections.Generic;
using StandardDeckManager.Scripts;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace StandardDeckManager.Demo.Scripts
{
    /// <summary>
    /// BlackjackManager
    /// Description: Manages and handles all functionality of the blackjack demo
    /// </summary>

    public class BlackjackManager : MonoBehaviour
    {
        #region Variables

        // public variables
        [Header("Game Objects")] public GameObject goDealerCardBorder; // where the dealer card spawns
        public GameObject goPlayerCardBorder; // where the player card spawns
        public GameObject goCardBackFace; // the card back face object

        [Header("UI Objects")] public Text txtDeckCount; // text object to track deck count
        public Text txtPlayerHandCount; // text object to track player's hand
        public Text txtDealerHandCount; // text object to track dealer's hand
        public Text txtPlayerScore; // text object to track player's score
        public Text txtDealerScore; // text object to track dealer's score
        public Text txtWinMessage; // text object to inform who won
        public Button btnHit; // hit button
        public Button btnStand; // stand button
        public Button btnPlayAgain; // replay button
        public Button btnMainMenu; // main menu button

        [Header("Other Settings")]
        public float fltWaitTimeAfterShuffle = 0.7f; // the wait time after the deck is shuffled

        public float fltWaitTimeBeforeDeal = 0.5f; // the wait time between when a dealer hits  
        public float fltWaitTimeBeforeResults = 0.5f; // the wait time before the winner is determined
        public float fltWaitTimeAfterHitButton = 0.6f; // the wait time for the button to appear again after hitting hit
        public Vector3 vecCardSpawnOffset; // how much offset when placing the cards next to each other

        [Header("Sound Effects")] public AudioSource audSrc; // the audio source to play sounds from
        public AudioClip audClpCardSlide; // audio clip for dealing a card
        public AudioClip audClpCardShuffle; // audio clip for shuffling the deck
        public AudioClip audClpWin; // audio clip for win state
        public AudioClip audClpLose; // audio clip for lose state
        public AudioClip audClpBlackjack; // audio clip for blackjack state
        public AudioClip audClpDraw; // audio clip for draw state

        [Header("Volume Levels")] public float fltCardSlideVolume = 0.5f; // the volume for card slide  
        public float fltCardShuffleVolume = 0.5f; // the volume for card shuffling   
        public float fltWinVolume = 0.5f; // the volume for our win sound
        public float fltLoseVolume = 0.5f; // the volume for our lose sound
        public float fltBlackjackVolume = 0.5f; // the volume for our blackjack sound
        public float fltDrawVolume = 0.5f; // the volume for our draw sound

        // private variables
        private List<Card> _mColDealerHand; // the dealer's hand
        private List<Card> _mColPlayerHand; // the player's hand
        private Vector3 _mVecDealerCardOffset; // the offset of where the card object is displayed for the dealer
        private Vector3 _mVecPlayerCardOffset; // the offset of where the card object is displayed for the player
        private int _mIntPlayerScore; // the player's score
        private int _mIntDealerScore; // the dealer's score
        private bool _mBlnActionInProgress; // check if the player performed an action already
        private bool _mBlnPlayerWins; // check if the player wins during a new deal set

        #endregion

        // on initialization
        private void Start()
        {
            // if the audio source is null
            if (audSrc == null)
            {
                // set it from this component
                audSrc = this.GetComponent<AudioSource>();
            }

            // spawn our back face object
            GameObject cardBackFace = Instantiate(goCardBackFace);
            cardBackFace.SetActive(false);
            cardBackFace.name = "Card Back Face";

            // set the card back face to the spawned object
            goCardBackFace = cardBackFace;

            // reset our button states
            btnHit.gameObject.SetActive(false);
            btnStand.gameObject.SetActive(false);
            btnPlayAgain.gameObject.SetActive(false);
            btnMainMenu.gameObject.SetActive(false);

            // update the deck count
            txtDeckCount.text = DeckManager.instance.CountDeck().ToString();

            // initialize the game
            StartCoroutine(InitializeGame());
        }

        #region Game Functionality

        // set up the deck
        private void SetUpDeck(List<Card> deck)
        {
            // for each card in the deck
            int i = 0;
            while (i < deck.Count)
            {
                // set up its value
                SetCardValue(deck[i]);
                i++;
            }
        }

        // sets the value for specific cards
        private void SetCardValue(Card card)
        {
            // create a switch statement for each 
            // rank type and return the value
            switch (card.rank)
            {
                case Card.Rank.Ace:
                    card.value = 1;
                    break;
                case Card.Rank.Two:
                    card.value = 2;
                    break;
                case Card.Rank.Three:
                    card.value = 3;
                    break;
                case Card.Rank.Four:
                    card.value = 4;
                    break;
                case Card.Rank.Five:
                    card.value = 5;
                    break;
                case Card.Rank.Six:
                    card.value = 6;
                    break;
                case Card.Rank.Seven:
                    card.value = 7;
                    break;
                case Card.Rank.Eight:
                    card.value = 8;
                    break;
                case Card.Rank.Nine:
                    card.value = 9;
                    break;
                case Card.Rank.Ten:
                    card.value = 10;
                    break;
                case Card.Rank.Jack:
                    card.value = 10;
                    break;
                case Card.Rank.Queen:
                    card.value = 10;
                    break;
                case Card.Rank.King:
                    card.value = 10;
                    break;
                default:
                    card.value = 0;
                    break;
            }
        }

        // initializes the game and handles the setup
        private IEnumerator InitializeGame()
        {
            yield return new WaitForSeconds(0.2f);

            // set up each deck's card value
            SetUpDeck(DeckManager.instance.deck);
            SetUpDeck(DeckManager.instance.discardPile);
            SetUpDeck(DeckManager.instance.inUsePile);

            // play our shuffle sfx
            AssignAudioClip(audClpCardShuffle);
            audSrc.Play();

            // shuffle the deck of cards
            DeckManager.instance.ShuffleDeck();

            yield return new WaitForSeconds(fltWaitTimeAfterShuffle);

            // reset the spawn offset
            ResetSpawnOffset();

            // mark player winning as false
            _mBlnPlayerWins = false;

            // deal a new hand 
            StartCoroutine(DealNewHand());
        }

        // deal a new hand to the player and dealer
        private IEnumerator DealNewHand()
        {
            // reset our variables
            _mBlnActionInProgress = false;

            // if there are cards in the in use pile
            if (DeckManager.instance.CountInUsePile() > 0)
                // put them in the discard pile
                DeckManager.instance.MoveAllCardToDiscard(DeckManager.instance.inUsePile);

            // check if the discard pile should 
            // be shuffled back into the main deck
            if (CheckForShuffle())
                yield return new WaitForSeconds(fltWaitTimeAfterShuffle);

            // create a new list for the dealer and player
            _mColDealerHand = new List<Card>();
            _mColPlayerHand = new List<Card>();

            // for 4 loops
            for (int i = 0; i < 4; i++)
            {
                // inform the manager an action is in progress
                _mBlnActionInProgress = true;

                // deal cards to both the dealer and player
                if (i % 2 == 0)
                {
                    StartCoroutine(DealCard(_mColDealerHand, goDealerCardBorder, false));

                    // while an action is in progress wait until it is complete
                    while (_mBlnActionInProgress)
                    {
                        yield return null;
                    }

                    // if this is the first deal
                    if (i == 0)
                    {
                        // display the current score of the dealer
                        CalculateDealerInitialHand();
                    }

                    yield return new WaitForSeconds(fltWaitTimeBeforeDeal);
                }
                else
                {
                    StartCoroutine(DealCard(_mColPlayerHand, goPlayerCardBorder, true));

                    // while an action is in progress wait until it is complete
                    while (_mBlnActionInProgress)
                    {
                        yield return null;
                    }

                    // display the current score of the player
                    CalculateHand(_mColPlayerHand, txtPlayerHandCount);

                    yield return new WaitForSeconds(fltWaitTimeBeforeDeal);
                }
            }

            // if the player did not win continue 
            if (!_mBlnPlayerWins)
            {
                // turn on our buttons
                btnHit.gameObject.SetActive(true);
                btnStand.gameObject.SetActive(true);
            }
            else
            {
                // force a stand
                StartCoroutine(Stand());
            }
        }

        // deal a card
        private IEnumerator DealCard(List<Card> hand, GameObject slot, bool isPlayer)
        {
            // check if the discard pile should 
            // be shuffled back into the main deck
            if (CheckForShuffle())
            {
                yield return new WaitForSeconds(fltWaitTimeAfterShuffle);
            }

            // add the card to the hand and set the sorting order
            hand.Add(DeckManager.instance.deck[0]);
            DeckManager.instance.deck[0].card.GetComponent<SpriteRenderer>().sortingOrder = hand.Count;

            // if we are dealing the player's card
            if (isPlayer)
            {
                // spawn the card and increment the offset
                SpawnCardToSlot(slot, DeckManager.instance.deck[0].card, _mVecPlayerCardOffset);
                _mVecPlayerCardOffset += vecCardSpawnOffset;
            }
            else
            {
                // if the dealer has no cards in their hand
                if (hand.Count == 1)
                {
                    // spawn the card and increment the offset
                    SpawnCardToSlot(slot, DeckManager.instance.deck[0].card, _mVecDealerCardOffset);
                    _mVecDealerCardOffset += vecCardSpawnOffset;
                }
                else if (hand.Count == 2)
                {
                    // spawn a backface card
                    SpawnBackFaceCardToSlot(slot, goCardBackFace, _mVecDealerCardOffset);
                    goCardBackFace.GetComponent<SpriteRenderer>().sortingOrder = hand.Count;
                    _mVecDealerCardOffset += vecCardSpawnOffset;
                }
                else
                {
                    SpawnCardToSlot(slot, DeckManager.instance.deck[0].card, _mVecDealerCardOffset);
                    _mVecDealerCardOffset += vecCardSpawnOffset;
                }
            }

            // assign the card sliding sfx
            AssignAudioClip(audClpCardSlide);
            audSrc.Play();

            // move the current card in the deck manager to the in use pile
            DeckManager.instance.MoveCardToInUse(DeckManager.instance.deck[0], DeckManager.instance.deck);

            // update the deck count
            txtDeckCount.text = DeckManager.instance.CountDeck().ToString();

            // set action in progress to false
            // to allow the game to continue
            _mBlnActionInProgress = false;
        }

        // spawn back face card onto slot
        private void SpawnBackFaceCardToSlot(GameObject slot, GameObject card, Vector3 offset)
        {
            card.transform.position = slot.transform.position + offset;
            card.SetActive(true);
        }

        // spawn the card onto the slots
        private void SpawnCardToSlot(GameObject slot, GameObject card, Vector3 offset)
        {
            card.transform.position = slot.transform.position + offset;
            card.SetActive(true);
        }

        // calculate the current hand
        private void CalculateHand(List<Card> hand, Text textOutput)
        {
            // create variables to reference the hand's cards
            var tIntScore = 0;
            var tBlnContainsAce = false;

            // for each card in the hand
            foreach (Card card in hand)
            {
                // get the card value and add it to the score
                tIntScore += card.value;

                // if the card is an ace flag it
                if (card.rank == Card.Rank.Ace)
                {
                    tBlnContainsAce = true;
                }
            }

            // if the hand contains an ace
            if (tBlnContainsAce)
            {
                // if the hand is less than or equal to 21 set the ace value to 11
                if ((tIntScore - 1) + 11 <= 21)
                {
                    tIntScore = (tIntScore - 1) + 11;
                }
            }

            // if the player has 21 indicate they won
            if (tIntScore == 21)
                _mBlnPlayerWins = true;

            // output our score onto the ui object
            textOutput.text = tIntScore.ToString();
        }

        // display only the face up value of the dealer's hand
        private void CalculateDealerInitialHand()
        {
            // output our score onto the ui object
            txtDealerHandCount.text = _mColDealerHand[0].value.ToString();
        }

        // stand and reveal the hands to determine who wins
        private IEnumerator Stand()
        {
            // hide the buttons
            btnHit.gameObject.SetActive(false);
            btnStand.gameObject.SetActive(false);

            // assign the card sliding sfx
            AssignAudioClip(audClpCardSlide);

            // spawn the dealer's second card onto the back face position
            _mColDealerHand[1].card.transform.position = goCardBackFace.transform.position;
            _mColDealerHand[1].card.GetComponent<SpriteRenderer>().sortingOrder = 2;
            _mColDealerHand[1].card.SetActive(true);
            goCardBackFace.SetActive(false);

            // play the sfx
            audSrc.Play();

            // calculate the dealer's hand
            CalculateHand(_mColDealerHand, txtDealerHandCount);

            yield return new WaitForSeconds(fltWaitTimeBeforeResults);

            // if the player's hand is less than or equal to 21
            if (int.Parse(txtPlayerHandCount.text) <= 21)
            {
                // if the dealer's hand is less than 17
                while (int.Parse(txtDealerHandCount.text) < 17)
                {
                    // inform the manager an action is in progress
                    _mBlnActionInProgress = true;

                    // add a new card to the dealer's hand 
                    StartCoroutine(DealCard(_mColDealerHand, goDealerCardBorder, false));

                    // while an action is in progress wait until it is complete
                    while (_mBlnActionInProgress)
                    {
                        yield return null;
                    }

                    // calculate the dealer's hand 
                    CalculateHand(_mColDealerHand, txtDealerHandCount);

                    // play the sfx
                    audSrc.Play();
                    yield return new WaitForSeconds(fltWaitTimeBeforeDeal);
                }
            }

            // reveal the score and who won
            SelectWinner();

            // show option buttons
            btnMainMenu.gameObject.SetActive(true);
            btnPlayAgain.gameObject.SetActive(true);
        }

        // add a card to the player hand
        private IEnumerator Hit()
        {
            // add a new card to the player hand 
            StartCoroutine(DealCard(_mColPlayerHand, goPlayerCardBorder, true));

            // while an action is in progress wait until it is complete
            while (_mBlnActionInProgress)
            {
                yield return null;
            }

            // calculate the player's hand 
            CalculateHand(_mColPlayerHand, txtPlayerHandCount);

            // check if the player's hand is greater than 21
            if (int.Parse(txtPlayerHandCount.text) > 21)
            {
                // hide the buttons
                btnHit.gameObject.SetActive(false);
                btnStand.gameObject.SetActive(false);

                // calculate the score so that the game is over
                yield return new WaitForSeconds(fltWaitTimeBeforeResults);
                StartCoroutine(Stand());
            }

            yield return new WaitForSeconds(fltWaitTimeAfterHitButton);
        }

        // reveal the score and who won
        private void SelectWinner()
        {
            // if the player score is 21 or less
            if (int.Parse(txtPlayerHandCount.text) <= 21)
            {
                // if the player score is higher than the dealer's
                if (int.Parse(txtPlayerHandCount.text) > int.Parse(txtDealerHandCount.text))
                {
                    // show that the player won and increment the score
                    if (int.Parse(txtPlayerHandCount.text) < 21)
                    {
                        txtWinMessage.text = "You have won!";
                        AssignAudioClip(audClpWin);
                    }
                    else
                    {
                        txtWinMessage.text = "Blackjack!";
                        AssignAudioClip(audClpBlackjack);
                    }

                    _mIntPlayerScore++;
                    txtPlayerScore.text = "You: " + _mIntPlayerScore;
                }
                else if (int.Parse(txtPlayerHandCount.text) == int.Parse(txtDealerHandCount.text))
                {
                    // show the it is a draw
                    txtWinMessage.text = "Draw!";
                    AssignAudioClip(audClpDraw);
                }
                else
                {
                    // if the dealer's hand is greater than 21
                    if (int.Parse(txtDealerHandCount.text) > 21)
                    {
                        txtWinMessage.text = "The dealer busts. You have won!";
                        _mIntPlayerScore++;
                        txtPlayerScore.text = "You: " + _mIntPlayerScore;
                        AssignAudioClip(audClpWin);
                    }
                    else
                    {
                        // show that the dealer won and increment the score
                        txtWinMessage.text = "The Dealer has won!";
                        _mIntDealerScore++;
                        txtDealerScore.text = "Dealer: " + _mIntDealerScore;
                        AssignAudioClip(audClpLose);
                    }
                }
            }
            else
            {
                // show that the dealer won and increment the score
                txtWinMessage.text = "Bust! The Dealer has won.";
                _mIntDealerScore++;
                txtDealerScore.text = "Dealer: " + _mIntDealerScore;
                AssignAudioClip(audClpLose);
            }

            audSrc.Play();
        }

        // assign an audio clip
        private void AssignAudioClip(AudioClip audClp)
        {
            // if the audio clip is not the clip we want
            if (audSrc.clip != audClp)
                // assign it
                audSrc.clip = audClp;

            // adjust the volume based on the clip
            if (audClp == audClpCardShuffle)
                audSrc.volume = fltCardShuffleVolume;
            else if (audClp == audClpCardSlide)
                audSrc.volume = fltCardSlideVolume;
            else if (audClp == audClpWin)
                audSrc.volume = fltWinVolume;
            else if (audClp == audClpLose)
                audSrc.volume = fltLoseVolume;
            else if (audClp == audClpDraw)
                audSrc.volume = fltDrawVolume;
            else if (audClp == audClpBlackjack)
                audSrc.volume = fltBlackjackVolume;
        }

        // reset the spawn offset
        private void ResetSpawnOffset()
        {
            _mVecDealerCardOffset = Vector3.zero;
            _mVecPlayerCardOffset = Vector3.zero;
        }

        // if there are no cards in the deck
        private bool CheckForShuffle()
        {
            // if there is less than the min amount of cards in the deck
            if (DeckManager.instance.CountDeck() > 0) return false;
            // shuffle the discard pile into the deck
            DeckManager.instance.ShuffleDecksTogether(DeckManager.instance.deck, DeckManager.instance.discardPile);

            // play the shuffle sfx
            AssignAudioClip(audClpCardShuffle);
            audSrc.Play();

            return true;

        }

        #endregion

        #region UI Button Actions

        // stand function for button click
        public void StandButton()
        {
            // if an action is already in progress
            if (_mBlnActionInProgress)
                // ignore everything else
                return;

            // inform the manager an action is in progress
            _mBlnActionInProgress = true;

            // stand and reveal the hands to determine who wins
            StartCoroutine(Stand());
        }

        // hit button
        public void HitButton()
        {
            // if an action is already in progress
            if (_mBlnActionInProgress)
                // ignore everything else
                return;

            // inform the manager an action is in progress
            _mBlnActionInProgress = true;

            // add a card to the player hand
            StartCoroutine(Hit());
        }

        // deal a new hand 
        public void PlayAgainButton()
        {
            // hide and show the appropriate buttons
            btnMainMenu.gameObject.SetActive(false);
            btnPlayAgain.gameObject.SetActive(false);

            // for each card in the player's hand
            foreach (var card in _mColPlayerHand)
            {
                // hide the card
                card.card.SetActive(false);
            }

            // for each card in the dealer's hand
            foreach (var card in _mColDealerHand)
            {
                // hide the card
                card.card.SetActive(false);
            }

            // reset the score
            txtDealerHandCount.text = "0";
            txtPlayerHandCount.text = "0";

            // remove the win message
            txtWinMessage.text = "";

            // reset the offsets
            ResetSpawnOffset();

            // mark the player has won as false
            _mBlnPlayerWins = false;

            // deal a new hand to the player and dealer
            StartCoroutine(DealNewHand());
        }

        // go to main menu
        public void MainMenuButton()
        {
            SceneManager.LoadScene("MainMenu");
        }

        #endregion
    }
}