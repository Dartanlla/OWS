---
layout: default
title: FAQ
nav_order: 2
has_children: false
---

# Frequently asked questions
{: .mb-5}

<details markdown="block">
  <summary class="fs-5 mb-3">
    How many players can OWS support?
  </summary>

With OWS 2.0 we try to  reach about 100.000 concurrent players. The exact possible player count is dependant on your game setup and features.
</details>

<details markdown="block">
  <summary class="fs-5 mb-3">
    Are transitions between Zone Server Instances seamless?
  </summary>

Currently this is not support in OWS 2.0. While seamless transitions are achievable by default Unreal Engine is not designed for it. OWS uses Unreal Engine's map travel system which by default unloads the current map before loading the new one. That said, you are free to implement your own logic without using Unreal Engine's client travel functionality to achieve a seamless transition experience.
</details>

<details markdown="block">
  <summary class="fs-5 mb-3">
    Can players on one Zone Server Instance see players on another Zone Server Instance?
  </summary>

They cannot by default. There is nothing stopping you from sending data between the Zone Server Instances but as of right now you need to develop that logic by yourself.
</details>

<details markdown="block">
  <summary class="fs-5 mb-3">
    How many players can each Zone Server Instance support?
  </summary>

As OWS is using Unreal Engine's server architecture the limitations are within their specifications. Epic released a [video](https://www.youtube.com/watch?v=CDnNAAzgltw) about optimizing the Data which gets send from and to the server using a custom Replication Graph. Fortnite, for example, supports up to 100 players per Zone Instance.
</details>