import { UseEntityFormOptions, useEntityForm } from "../Form";

export interface UseViewFiltersProps extends Omit<UseEntityFormOptions, 'noSaveOnSubmit'> {

}

export function useViewFiltersForm(props: UseViewFiltersProps) {
    const formAPI = useEntityForm({
        noSaveOnSubmit: true,
        ...props
    });

    return formAPI;
}