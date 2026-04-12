// [기획 설계] 플레이어의 현재 상태를 정의하여 로직 간의 충돌을 방지함.
// Idle(대기), Moving(이동), Casting(즉시 시전), Charging(차징/기모으기) 상태를 구분하여
// 특정 상태에서 다른 행동이 불가능하도록 제어하는 '상태 머신'의 기초 데이터로 활용함.
// 
public enum PlayerState { Idle, Moving, Casting, Charging }