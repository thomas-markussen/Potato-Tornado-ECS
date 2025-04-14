# POTATO TORNADO
## A simulation of too many potatoes and a tornado

Potato Tornado is a project in Unity which utilizes the Entity Component System paradigm to simulate hundreds of thousands of entities at the same time. 
![tornado potato](https://github.com/user-attachments/assets/455b1fbc-ea5e-4a1a-8282-443c87a055da)

# CONFIGURATION

Our simulation is very customizable due to the countless algorithms and real-life physics
formulas that we utilize to bring the simulation to life. To control these variables, we have
created a singleton component called Config, which is attached to a GameObject, also
called Config, present in the subscene. This configuration component contains a dozen
parameters to control how the potatoes behave in relation to the tornado, as well as the
size and shape of the tornado. Most of these variables can be updated at runtime. There
are so many ways of making the tornado do crazy things, and the patterns of its inner
workings shows some amazing patterns at times as well.

![image](https://github.com/user-attachments/assets/42c8d29f-f6d2-497c-ad85-444f60838f82)
