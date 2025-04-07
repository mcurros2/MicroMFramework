import { Value, ValuesObject } from "../src";

const MNamesBank = ['Joseph', 'Louis', 'Mary', 'Angel', 'Victor', 'Gael', 'Mark', 'Emmanuel', 'James', 'Mariano', 'John', 'Nicholas', 'George', 'Eric', 'Matthew', 'Santiago'];
const FNamesBank = ['Victoria', 'Matilda', 'Christina', 'Cynthia', 'Andrea', 'Camila', 'Elsa', 'Laura', 'Tatiana', 'Mary', 'Anna', 'Sophia', 'Samantha', 'Giselle', 'Jimena', 'Luciana'];
const SurnamesBank = ['Smith', 'Johnson', 'Williams', 'Brown', 'Jones', 'Garcia', 'Miller', 'Davis', 'Rodriguez', 'Martinez', 'Hernandez', 'Lopez', 'Gonzalez', 'Wilson', 'Anderson', 'Thomas', 'Taylor'];

export function RandomIntFromInterval(min: number, max: number) { // min and max included 
    return Math.floor(Math.random() * (max - min + 1) + min)
}

export function RandomPhraseFromBank(bank: string[], wordsCount: number) {
    let result = ""
    for (let i = 0; i < wordsCount; i++) {
        result = result.concat((i > 0 ? " " : "") + bank[Math.floor(Math.random() * bank.length)])
    }
    return result
}

export function RandomNameM(countMin: number, countMax: number) {
    return RandomPhraseFromBank(MNamesBank, RandomIntFromInterval(countMin, countMax));
}

export function RandomNameF(countMin: number, countMax: number) {
    return RandomPhraseFromBank(FNamesBank, RandomIntFromInterval(countMin, countMax));
}

export function RandomSurname() {
    return RandomPhraseFromBank(SurnamesBank, 1);
}

export function RandomFullNameM() {
    return RandomSurname() + ', ' + RandomNameM(1, 2);
}

export function RandomFullNameF() {
    return RandomSurname() + ', ' + RandomNameF(1, 2);
}

export function RandomFullName() {
    return (RandomIntFromInterval(0, 1) ? RandomFullNameF() : RandomFullNameM());
}

export function RandomDate() {
    const start = new Date(1999, 0, 1);
    const end = new Date(2030, 0, 1);
    return new Date(start.getTime() + Math.random() * (end.getTime() - start.getTime()));
}

export function RandomRecords<T>(count:number, factory:() => T) {
    return Array(count).fill(null).map(() => factory());
}

export function RandomHexColor() {
    return Math.floor(Math.random()*16777215).toString(16);
}
