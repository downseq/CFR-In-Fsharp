﻿// Intended to test eq 9 from the `Regret Minimization in Games with Incomplete Information` paper with some simplifying assumptions.
// * Chance nodes are not there.
// * T is 1.
// * Policy is not split into actions at current node and everything else.
// * Only a single player is considered. This is equivalent to the other player having nodes with only a single action.

// All of the above are simplifications of the original definitions and could easily be added. But if the subset does not work, then
// the full proof won't either.

#load "Core.fsx"
open Core

let u' (o : Policy) f = function
    | Terminal reward -> reward
    | Response (id, branches) -> Array.fold2 (fun s policy branch -> s + f policy branch) 0.0 o.[id] branches

let rec u (o : Policy) tree = u' o (fun policy branch -> policy * u o branch) tree

let update_at_branch_current cur next = function
    | Terminal _ -> cur
    | Response (id, _) -> Map.add id (Map.find id next) cur

let action_at i (o : Policy) (id : Infoset) =
    let a = Array.zeroCreate o.[id].Length
    a.[i] <- 1.0
    Map.add id a o

let action_max (o : Policy) (id : Infoset) f =
    let len = o.[id].Length
    let rec loop s i =
        if i < len then
            let a = Array.zeroCreate len
            a.[i] <- 1.0
            loop (max s (f (Map.add id a o))) (i+1)
        else s
    loop -infinity 0

let R (o' : Policy) (o : Policy) (tree : GameTree) = u o' tree - u o tree
let R' (o' : Policy) (o : Policy) (tree : GameTree) =
    match tree with
    | Terminal _ -> 0.0
    | Response (id, branches) ->
        action_max o id (fun o' -> u o' tree - u o tree) + 
        // According to definition of succ which only considers opponent reach probabilities, 
        // I skip multipling by current player reach probabilities.
        // Since perfect recall assures no cycles in infosets `u o' branch = u (action_at i o' id) branch` holds.
        Array.max (Array.mapi (fun i branch -> u (action_at i o' id) branch - u o branch) branches)

open FsCheck

let ``R=R'`` ({tree=tree; policies=policies} : TreePolicies) =
    let o, o' = policies.[0], policies.[1]
    let left = R o' o tree
    let right = R' o' o tree
    // Tests for equality and prints an error if the property fails
    left <= right |@ sprintf "%f <> %f" left right

// Fails.
Check.One({Config.Default with MaxTest=10000}, ``R=R'``)