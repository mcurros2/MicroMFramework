import { AddressSearch } from "../../src/UI/AddressInput/AddressSearch";


export function AddressSearchTest() {

    return (
        <AddressSearch countries={['us']} initialSearchAddress="Pizza" />
    )
}