# CYD Commander — Hardware Assembly Guide
### Soldered Prototype Build — MTG Commander Life Tracker

---

## Table of Contents

1. [Parts List](#parts-list)
2. [Tools Required](#tools-required)
3. [Pre-Build Safety](#pre-build-safety)
4. [Master Wiring Diagram](#master-wiring-diagram)
5. [Stage 1 — Battery & Charging](#stage-1--battery--charging-tp4056)
6. [Stage 2 — Boost Converter](#stage-2--boost-converter-mt3608)
7. [Stage 3 — Power Switch](#stage-3--power-switch)
8. [Stage 4 — CYD Power Input](#stage-4--cyd-power-input-via-micro-usb)
9. [Stage 5 — Haptic Feedback Circuit](#stage-5--haptic-feedback-circuit)
10. [Final System Verification](#final-system-verification)
11. [Troubleshooting](#troubleshooting)

---

## Parts List

### Power System
| # | Part | Quantity |
|---|------|----------|
| 1 | CYD ESP32-3248S035 (3.5" display) | 1 |
| 2 | 3.7V LiPo battery (1000mAh+) | 1 |
| 3 | TP4056 charging module (Type-C) | 1 |
| 4 | MT3608 boost converter | 1 |
| 5 | SPDT slide switch (3-pin) | 1 |
| 6 | Micro USB male pigtail (2-wire) | 1 |
| 7 | 1000µF 10V electrolytic capacitor | 1 |

### Haptic Feedback System
| # | Part | Quantity | Identification |
|---|------|----------|----------------|
| 8 | Coin vibration motor (3V) | 1 | Red & black leads |
| 9 | 2N7000 N-channel MOSFET (TO-92) | 1 | Marked "2N7000" |
| 10 | 1kΩ resistor | 1 | Brown-Black-Red bands |
| 11 | 10kΩ resistor | 1 | Brown-Black-Orange bands |
| 12 | 1N4148 diode | 1 | Black stripe = cathode |

### Wire & Materials
| # | Part | Quantity |
|---|------|----------|
| 13 | 22 AWG solid wire (red/black/colored) | ~3 feet |
| 14 | Heat shrink tubing assorted | 1 pack |
| 15 | Solder (60/40 rosin core) | — |

---

## Tools Required

- Soldering iron (350°C / 660°F)
- Solder wick or desoldering pump
- Wire strippers (22 AWG capable)
- Side cutters / flush cutters
- Digital multimeter
- Helping hands / PCB vise (recommended)
- Heat gun or lighter (for heat shrink)
- Tweezers
- Small Phillips screwdriver (for MT3608 trimpot)

---

## Pre-Build Safety

⚠️ **CRITICAL WARNINGS:**

- **NEVER short LiPo battery terminals** — instant fire risk
- **Disconnect battery during all soldering** — until explicitly told to connect
- **Polarity matters** — reversed electrolytic capacitors can explode
- **Set MT3608 voltage BEFORE connecting CYD** — over-voltage destroys the board
- **Insulate every joint with heat shrink** — prevents shorts during use

---

## Master Wiring Diagram

```
┌──────────┐
│  LiPo    │
│  3.7V    │
└─┬──────┬─┘
  │      │
 RED   BLACK
  │      │
  ▼      ▼
┌──────────┐
│  TP4056  │ ◄── USB-C charges here
│          │
│ OUT+ OUT-│
└─┬──────┬─┘
  │      │
  ▼      ▼
┌──────────┐
│  MT3608  │     +1000µF cap across OUT+/OUT-
│          │
│ OUT+ OUT-│
└─┬──────┬─┘
  │      │
  ▼      │
┌──────┐ │
│SWITCH│ │     (middle pin = input)
└─┬────┘ │
  │      │
  ▼      ▼
┌──────────┐
│  Micro   │
│  USB     │ ────► Plugs into CYD
│  Pigtail │
└──────────┘

──────────────────────────────────────────────

HAPTIC CIRCUIT (powered from CYD's 3.3V/GND):

CYD 3.3V ───────┬─── Motor (RED)
                │
            [1N4148]   ◄── stripe toward 3.3V
                │
                └─── Motor (BLACK)
                          │
                          ▼
                       [Drain] = Pin 3
                       2N7000
                       [Source] = Pin 1 ─── CYD GND

CYD GPIO22 ── [1kΩ] ── [Gate] = Pin 2 ─── [10kΩ] ─── CYD GND
```

---

# Stage 1 — Battery & Charging (TP4056)

### TP4056 Module Pin Reference

```
   ┌──────────────────────────┐
   │                          │
   │   ┌──────┐               │
   │   │ USB  │  ●RED LED     │
   │   │ TYPE │  ●BLUE LED    │
   │   │  C   │               │
   │   └──────┘               │
   │                          │
   │  [B+] [B-]    [OUT+][OUT-]│
   └──┬───┬─────────┬───┬──────┘
      │   │         │   │
   battery        output to
                  MT3608
```

### Step 1.1 — Connect LiPo to TP4056

| From | To | Wire Color |
|------|-----|-----------|
| **LiPo battery (+) RED** | **TP4056 B+ pad** | RED |
| **LiPo battery (-) BLACK** | **TP4056 B- pad** | BLACK |

**Procedure:**
1. Strip ~3mm from each battery wire
2. Tin both the wire ends and the B+/B- pads (apply tiny solder dot)
3. Solder battery RED → B+
4. Solder battery BLACK → B-
5. Cover each joint with heat shrink

### 🧪 Multimeter Test 1.1

**Setup:** Multimeter on **DC Volts**, range **20V** or **6000m**

| Probe Red | Probe Black | Expected Reading |
|-----------|-------------|------------------|
| TP4056 **B+** pad | TP4056 **B-** pad | **3.0 – 4.2 V** |

✅ **PASS:** Reading between 3000 and 4200 mV  
❌ **FAIL:** Reading 0V or negative — check polarity, redo joints

### 🧪 Multimeter Test 1.2 — Charging

**Setup:** Plug USB-C into TP4056

| Indicator | Meaning |
|-----------|---------|
| RED LED on TP4056 | Charging ✅ |
| BLUE/GREEN LED on TP4056 | Fully charged ✅ |
| No LED | FAIL — check USB cable & solder joints |

---

# Stage 2 — Boost Converter (MT3608)

### MT3608 Module Pin Reference

```
   ┌────────────────────────┐
   │                        │
   │   [● TRIMPOT screw]    │  ◄── voltage adjustment
   │                        │
   │  [IN+] [IN-]           │
   │  [OUT+][OUT-]          │
   └──┬─────┬───┬─────┬─────┘
      │     │   │     │
    input   │  output │
    from    │  to     │
    TP4056  │  switch │
```

### Step 2.1 — Connect TP4056 Output to MT3608 Input

| From | To | Wire Color |
|------|-----|-----------|
| **TP4056 OUT+** | **MT3608 IN+** | RED |
| **TP4056 OUT-** | **MT3608 IN-** | BLACK |

**Procedure:**
1. Cut two ~3 inch wires (red and black)
2. Strip and tin both ends of each
3. Solder one end to TP4056 OUT pads
4. Solder other end to MT3608 IN pads
5. Heat shrink both joints

### 🧪 Multimeter Test 2.1 — MT3608 Receiving Power

| Probe Red | Probe Black | Expected Reading |
|-----------|-------------|------------------|
| MT3608 **IN+** | MT3608 **IN-** | **3.0 – 4.2 V** |

✅ **PASS:** Same as battery voltage  
❌ **FAIL:** Check OUT+/OUT- wire continuity

---

### Step 2.2 — Add Smoothing Capacitor

**⚠️ POLARITY MATTERS** — wrong direction = explosion risk

| From | To |
|------|-----|
| **1000µF cap LONG leg (+)** | **MT3608 OUT+** pad |
| **1000µF cap SHORT leg (-)** | **MT3608 OUT-** pad |

The negative leg is marked with a **stripe** on the capacitor body. Match the stripe to the (-) side.

**Procedure:**
1. Trim cap legs to ~10mm
2. Solder LONG leg directly onto MT3608 OUT+ pad
3. Solder SHORT leg directly onto MT3608 OUT- pad
4. Cap sits right on top of the module

---

### Step 2.3 — Set MT3608 Output to 5V

**⚠️ DO THIS BEFORE CONNECTING ANYTHING DOWNSTREAM**

1. Set multimeter to **DC V, 6000m range**
2. **Red probe** → MT3608 **OUT+** pad
3. **Black probe** → MT3608 **OUT-** pad
4. Use small screwdriver on the **brass trimpot**
5. Turn **CLOCKWISE** slowly
6. May take **15-25 full turns** to start rising

### 🧪 Multimeter Test 2.3 — Target Voltage

| Reading (mV) | Action |
|--------------|--------|
| Below 4900 | Turn trimpot clockwise more |
| **5000 – 5100** | ✅ **TARGET — STOP HERE** |
| 5100 – 5200 | Acceptable, slightly high |
| Above 5200 | Turn counter-clockwise to lower |
| "OL" displayed | Voltage > 6V — switch to 60V range, turn DOWN |

**Lock in the setting** by leaving the trimpot alone for the rest of the build.

---

# Stage 3 — Power Switch

### SPDT Slide Switch Pin Reference

```
      ┌─────────────┐
      │   ──────    │  ◄── slider
      │             │
      └─┬───┬───┬───┘
        │   │   │
       Pin1 Pin2 Pin3
       (out)(IN)(out)
```

**KEY:** Middle pin is **always** the input. Outer pins are the two outputs (slider selects which one).

### Step 3.1 — Wire Switch Input

| From | To | Wire Color |
|------|-----|-----------|
| **MT3608 OUT+** | **Switch MIDDLE pin (Pin 2)** | RED |

**Procedure:**
1. Cut ~4 inch red wire
2. Solder one end to MT3608 OUT+
3. Solder other end to switch middle pin
4. Heat shrink the switch pin solder joint

### Step 3.2 — Wire Switch Output

Choose ONE outer pin (Pin 1 OR Pin 3 — doesn't matter which, slider direction will just be reversed).

This output pin will connect to the micro USB pigtail in Stage 4.

### 🧪 Multimeter Test 3.1 — Switch Function

**Probe SETUP:** Red on **chosen outer pin**, Black on **MT3608 OUT-**

| Switch Position | Expected Reading |
|----------------|------------------|
| **ON** (slider toward chosen outer pin) | **5000 – 5100 mV** |
| **OFF** (slider toward other side) | **0 mV** |

✅ **PASS:** Reading toggles between 5V and 0V cleanly  
❌ **FAIL:** Switch broken or solder joint bad

---

# Stage 4 — CYD Power Input (via Micro USB)

### Why Micro USB and Not VIN Pins?

The CYD's internal CH340 USB-to-serial chip needs the USB connector's V+ pin powered to put the ESP32 in normal boot mode. Powering through VIN bypasses this and causes a boot loop. The micro USB pigtail solves this cleanly.

### Micro USB Pigtail Reference

```
   ┌─────────┐         ╔═══╗
   │  RED    │ ◄── 5V  ║ ┌─┐
   │  BLACK  │ ◄── GND ║ │ │  ◄── Micro USB plug
   └─────────┘         ║ └─┘
                       ╚═══╝
```

### Step 4.1 — Connect Pigtail Wires

| From | To | Wire Color |
|------|-----|-----------|
| **Switch chosen outer pin** | **Pigtail RED wire** | RED |
| **MT3608 OUT-** | **Pigtail BLACK wire** | BLACK |

**Procedure:**
1. Solder switch outer pin → pigtail RED
2. Solder MT3608 OUT- → pigtail BLACK
3. Heat shrink both joints
4. **Plug micro USB end into CYD's micro USB port**

### 🧪 Multimeter Test 4.1 — Power at the Plug

**Setup:** Probe the micro USB pigtail solder joints (or backprobe the connector)

| Switch Position | Probe Red | Probe Black | Expected Reading |
|----------------|-----------|-------------|------------------|
| ON | Pigtail RED wire | Pigtail BLACK wire | **5000 – 5100 mV** |
| OFF | Pigtail RED wire | Pigtail BLACK wire | **0 mV** |

### 🧪 Multimeter Test 4.2 — Boot Test

**Setup:** Plug pigtail into CYD, switch ON

| Observation | Result |
|-------------|--------|
| CYD displays MTG setup screen within ~2 seconds | ✅ PASS |
| CYD shows boot loop (flashing) | ❌ Check pigtail orientation, voltage |
| CYD stays black | ❌ Check connections, multimeter at every step |

---

# Stage 5 — Haptic Feedback Circuit

### 2N7000 MOSFET Pinout

**Hold MOSFET with FLAT printed side facing you, leads pointing DOWN:**

```
        ┌─────────┐
        │ 2N7000  │  ◄── label readable
        │         │
        └─┬──┬──┬─┘
          │  │  │
        Pin1 Pin2 Pin3
         S   G   D
       SOURCE GATE DRAIN
```

### Step 5.1 — Wire the MOSFET Source

| From | To | Wire Color |
|------|-----|-----------|
| **2N7000 Pin 1 (Source)** | **CYD GND pin** (from CN1 connector) | BLACK |

**Procedure:**
1. Solder a short wire to MOSFET Pin 1
2. Other end to CYD's GND (CN1 pin 1 OR P3 pin 1)
3. Heat shrink

### Step 5.2 — Wire the Pull-Down Resistor (10kΩ)

**This is the critical resistor that prevents the motor from running constantly.**

10kΩ resistor (Brown-Black-Orange bands):

| From | To |
|------|-----|
| **One leg of 10kΩ** | **2N7000 Pin 2 (Gate)** |
| **Other leg of 10kΩ** | **CYD GND** (same GND as Source) |

**Procedure:**
1. Solder 10kΩ resistor between Gate pin and GND
2. Keep leads short
3. Heat shrink to prevent the bare resistor body from shorting

### Step 5.3 — Wire the Gate Control Resistor (1kΩ)

1kΩ resistor (Brown-Black-Red bands):

| From | To | Wire Color |
|------|-----|-----------|
| **CYD GPIO22 pin** | **One leg of 1kΩ** | YELLOW (or any) |
| **Other leg of 1kΩ** | **2N7000 Pin 2 (Gate)** | direct |

**GPIO22 is found on CN1 connector pin 2.**

**Procedure:**
1. Solder one resistor leg directly to the Gate pin
2. Solder the other leg to a short wire
3. Wire goes to CYD's GPIO22 (CN1 pin 2)
4. Heat shrink

### Step 5.4 — Wire the Motor

| From | To | Wire Color |
|------|-----|-----------|
| **Motor RED wire (+)** | **CYD 3.3V pin** (CN1 pin 4) | RED |
| **Motor BLACK wire (-)** | **2N7000 Pin 3 (Drain)** | BLACK |

**Procedure:**
1. Solder motor RED to CYD 3.3V
2. Solder motor BLACK to MOSFET Drain pin
3. Heat shrink both

### Step 5.5 — Wire the Flyback Diode (1N4148)

**⚠️ POLARITY CRITICAL** — backwards diode = short circuit

The 1N4148 has a **black stripe** on one end = **cathode**.

| From | To |
|------|-----|
| **Diode ANODE** (plain end) | **2N7000 Pin 3 (Drain)** |
| **Diode CATHODE** (striped end) | **CYD 3.3V pin** |

The diode goes **in parallel with the motor**, stripe pointing toward 3.3V.

**Procedure:**
1. Position diode across motor wires
2. Stripe end faces 3.3V side
3. Solder both leads
4. Heat shrink

---

### Final Haptic Circuit Reference

```
CYD 3.3V ────┬───────── Motor (RED +)
             │
        ◄[1N4148]◄ stripe up
             │
             │           Motor (BLACK -)
             │                 │
             │                 ▼
             │           [Pin 3 = Drain]
             │              2N7000
             │           [Pin 1 = Source]
             │                 │
             │                 ▼
             │             CYD GND
             │
CYD GPIO22 ─[1kΩ]─ [Pin 2 = Gate]
                         │
                       [10kΩ]
                         │
                       CYD GND
```

### 🧪 Multimeter Test 5.1 — Idle State (CYD Off)

| Probe Red | Probe Black | Expected Reading |
|-----------|-------------|------------------|
| 2N7000 Gate (Pin 2) | CYD GND | **0 mV** |
| 2N7000 Source (Pin 1) | CYD GND | **0 mV** (continuity) |

✅ **PASS:** Gate is at 0V — pull-down resistor working  
❌ **FAIL:** Gate reads anything else — check 10kΩ resistor connections

### 🧪 Multimeter Test 5.2 — With Test Sketch Running

Upload the haptic test sketch. With multimeter probing **Gate to GND**:

| Sketch State | Expected Reading |
|--------------|------------------|
| Buzz period (100ms) | **~3300 mV** (3.3V) |
| Quiet period (2s) | **0 mV** |

✅ **PASS:** Voltage toggles cleanly between 0V and 3.3V  
❌ **FAIL:** Stuck at one voltage — check GPIO22 wire & 1kΩ resistor

### 🧪 Multimeter Test 5.3 — Motor Current Draw

**Optional but useful:** Set multimeter to **DC Amps (200mA range)**, in series with motor BLACK wire.

| Sketch State | Expected Reading |
|--------------|------------------|
| Buzz active | **40 – 80 mA** |
| Quiet | **0 mA** |

---

# Final System Verification

### Complete Power-On Test

| Step | Action | Expected Result | ✓ |
|------|--------|----------------|---|
| 1 | Battery connected, switch OFF | CYD dark, no LEDs | ☐ |
| 2 | Flip switch ON | CYD boots within ~2 seconds | ☐ |
| 3 | Tap touchscreen | UI responds | ☐ |
| 4 | Adjust life count | Number changes | ☐ |
| 5 | Touch +/- | Motor buzzes briefly | ☐ |
| 6 | Flip switch OFF | CYD turns off cleanly | ☐ |
| 7 | Plug USB-C into TP4056 | RED LED lights, battery charges | ☐ |
| 8 | Wait for full charge | LED turns BLUE/GREEN | ☐ |

### Final Voltage Reference Sheet

Use these readings during operation to verify everything is healthy:

| Test Point | Probe Red | Probe Black | Expected | Notes |
|------------|-----------|-------------|----------|-------|
| Battery voltage | B+ | B- | 3.0 – 4.2V | Lower = needs charge |
| TP4056 output | OUT+ | OUT- | Same as battery | |
| MT3608 input | IN+ | IN- | Same as battery | |
| MT3608 output | OUT+ | OUT- | 5.0 – 5.1V | Should stay stable under load |
| Switch output | Outer pin | OUT- | 5.0 – 5.1V (ON) | 0V when OFF |
| At CYD USB | Pigtail RED | Pigtail BLACK | 5.0 – 5.1V | |
| MOSFET Gate (idle) | Pin 2 | GND | 0V | Pull-down working |
| MOSFET Gate (active) | Pin 2 | GND | 3.3V | GPIO22 driving |

---

# Troubleshooting

### Issue: CYD won't boot, just flashes

| Likely Cause | Test | Fix |
|--------------|------|-----|
| Powering through VIN instead of USB | Check pigtail is in USB port | Use micro USB pigtail |
| MT3608 voltage too low under load | Probe MT3608 OUT during boot | Re-adjust trimpot to 5.05V |
| Capacitor not seated | Visual check | Re-solder cap polarity correct |
| Battery low | Probe B+ to B- | Charge battery |

### Issue: Motor runs constantly

| Likely Cause | Test | Fix |
|--------------|------|-----|
| Missing 10kΩ pull-down | Probe Gate to GND (should be 0V) | Verify pull-down solder joints |
| MOSFET orientation wrong | Check flat side / pin order | Reverse pin assignments |
| Damaged MOSFET | Replace 2N7000 | New MOSFET |

### Issue: Motor never runs

| Likely Cause | Test | Fix |
|--------------|------|-----|
| Flyback diode reversed | Visual stripe check | Reverse diode |
| GPIO22 not connected | Probe Gate when sketch buzzes | Repair wire to GPIO22 |
| Motor wires reversed | Swap motor leads | Re-solder |
| Drain to Source open | Check MOSFET pinout | Verify 2N7000 markings |

### Issue: MT3608 won't reach 5V

| Likely Cause | Test | Fix |
|--------------|------|-----|
| Not enough turns on trimpot | Count turns (need 15-25) | Keep turning clockwise |
| No input voltage | Probe IN+ to IN- | Fix TP4056 connections |
| Defective module | Try another MT3608 | Replace module |

### Issue: TP4056 won't charge

| Likely Cause | Test | Fix |
|--------------|------|-----|
| Battery polarity reversed | Probe B+ to B- (should be positive) | Swap battery wires |
| USB cable not data/power | Try known-good cable | New USB-C cable |
| Battery dead | Probe battery voltage | Replace battery |

---

# Specifications

| Parameter | Value |
|-----------|-------|
| Battery voltage range | 3.0 – 4.2V |
| Battery capacity | 1000mAh (recommended minimum) |
| Charging current | ~1A via USB-C |
| Charging time | 1-2 hours |
| MT3608 output | 5.0 – 5.1V regulated |
| CYD operating current | ~115mA active |
| Motor current | 40-80mA when buzzing |
| Estimated runtime | 3-5 hours continuous |

---

*Hardware Assembly Guide v2.0 — CYD Commander Project*
