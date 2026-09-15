import { ReactNode } from "react";
import { Entity, EntityDefinition } from "../../Entity";
import { AutoFiltersForm } from "../AutoFiltersForm";
import { AutoForm } from "../AutoForm";
import { FormOptions } from "./types";

export async function createEntityForm<T extends FormOptions<Entity<EntityDefinition>>>(props: T): Promise<ReactNode> {

    const { entity, getDataOnInit, initialFormMode } = props;

    const effectiveGetDataOnInit = getDataOnInit !== undefined ? getDataOnInit : initialFormMode !== 'add';

    if (!entity.Form) throw new Error("Entity does not have a form");
    let entity_form: ReactNode;

    const effectiveProps = {
        ...props,
        getDataOnInit: effectiveGetDataOnInit,
        initialFormMode
    };

    if (entity.Form === "AutoForm") {
        entity_form = <AutoForm {...effectiveProps} />
    }
    else if (entity.Form === "AutoFiltersForm") {
        entity_form = <AutoFiltersForm {...effectiveProps} />
    }
    else {
        entity_form = await entity.Form!.then(DynamicForm =>
            <DynamicForm {...effectiveProps} />
        )
    }

    return entity_form;
};
